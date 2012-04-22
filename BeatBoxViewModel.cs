using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.DirectX.DirectSound;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using System.Timers;
using System.IO;

namespace BeatBox
{
    internal sealed class BeatBoxViewModel : ViewModelBase
    {
        private int _bpm;
        private readonly ICollectionView _devices;
        private readonly ICommand _playCommand;
        private StreamPlayer _player;
        private readonly ICommand _stopCommand;
        private readonly List<TrackViewModel> _tracks;
        private readonly IntPtr _windowHandle; // unless DirectSound is abstracted away, this is necessary

        public BeatBoxViewModel(IntPtr windowHandle)
        {
            List<DeviceViewModel> devices = new List<DeviceViewModel>();
            DevicesCollection availableDevices = new DevicesCollection();
            foreach (DeviceInformation availableDevice in availableDevices)
            {
                DeviceViewModel device = new DeviceViewModel(availableDevice.DriverGuid, availableDevice.ModuleName, availableDevice.Description);
                devices.Add(device);
            }

            _devices = CollectionViewSource.GetDefaultView(devices);
            _bpm = 120;
            _playCommand = new DelegateCommand(Play);
            _stopCommand = new DelegateCommand(Stop);
            _tracks = new List<TrackViewModel>();
            _tracks.Add(new TrackViewModel("Bass", @"samples\bass.wav", new bool[] { true, false, false, true, true, false, false, false, true, false, false, true, true, false, false, false }));
            _tracks.Add(new TrackViewModel("Open", @"samples\open.wav", new bool[] { false, false, false, false, false, false, false, true, false, false, false, false, false, false, false, true }));
            _tracks.Add(new TrackViewModel("Snare", @"samples\snare.wav", new bool[] { false, false, true, false, false, false, true, false, false, false, true, false, false, false, true, false }));
            _tracks.Add(new TrackViewModel("Closed", @"samples\closed.wav", new bool[] { true, true, false, true, true, true, false, false, true, true, false, true, true, true, false, false }));
            _windowHandle = windowHandle;
        }

        /// <summary>
        /// Gets or sets the number of beats per minute of the mix.
        /// </summary>
        public int BeatsPerMinute
        {
            get { return _bpm; }
            set
            {
                _bpm = value;
                OnPropertyChanged("BeatsPerMinute");
            }
        }

        /// <summary>
        /// Gets the list of devices available for playback.
        /// </summary>
        public ICollectionView Devices
        {
            get { return _devices; }
        }

        /// <summary>
        /// Gets the command which starts playback.
        /// </summary>
        public ICommand PlayCommand
        {
            get { return _playCommand; }
        }

        /// <summary>
        /// Gets the command which stops playback.
        /// </summary>
        public ICommand StopCommand
        {
            get { return _stopCommand; }
        }

        /// <summary>
        /// Gets the list of tracks to be mixed.
        /// </summary>
        public IEnumerable<TrackViewModel> Tracks
        {
            get { return _tracks; }
        }

        /// <summary>
        /// Starts playback on the currently selected device with the current tracks.
        /// </summary>
        /// <param name="parameter"></param>
        private void Play(object parameter)
        {
            DeviceViewModel currentDevice = (DeviceViewModel)_devices.CurrentItem;
            if (currentDevice != null)
            {
                if (_player != null)
                {
                    _player.Stop();
                    _player.Dispose();
                    _player = null;
                }

                Device playbackDevice = new Device(currentDevice.DriverGuid);
                playbackDevice.SetCooperativeLevel(_windowHandle, CooperativeLevel.Normal);

                Track[] tracks = new Track[_tracks.Count];
                int t = 0;
                foreach (TrackViewModel track in _tracks)
                {
                    tracks[t] = new Track(WaveData.FromFile(track.FilePath), track.GetBeatArray());
                    t++;
                }

                BeatBoxMixer mixer = new BeatBoxMixer(_bpm, tracks);                
                _player = new StreamPlayer(playbackDevice, tracks[0].Format, mixer);
                _player.Play();
            }
        }

        /// <summary>
        /// Stops playback.
        /// </summary>
        /// <param name="parameter"></param>
        private void Stop(object parameter)
        {
            if (_player != null)
            {
                _player.Stop();
                _player.Dispose();
                _player = null;
            }
        }
    }

    internal sealed class DeviceViewModel : ViewModelBase
    {
        private readonly string _description;
        private readonly Guid _driverGuid;
        private readonly string _name;

        public DeviceViewModel(Guid driverGuid, string name, string description)
        {
            _driverGuid = driverGuid;
            _name = name;
            _description = description;
        }

        public string Description
        {
            get { return _description; }
        }

        public Guid DriverGuid
        {
            get { return _driverGuid; }
        }

        public string Name
        {
            get { return _name; }
        }

        public override string ToString()
        {
            return string.Format("{0}: {1} ({2})", _name, _description, _driverGuid);
        }
    }

    internal sealed class TrackViewModel : ViewModelBase
    {
        public TrackViewModel(string name, string filePath, bool[] beats)
        {
            Name = name;
            FilePath = filePath;
            Beats = new List<BeatViewModel>();
            foreach (bool beat in beats)
            {
                Beats.Add(new BeatViewModel(beat));
            }
        }

        public List<BeatViewModel> Beats
        {
            get;
            private set;
        }

        public string FilePath
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            private set;
        }

        public bool[] GetBeatArray()
        {
            bool[] beats = new bool[Beats.Count];
            int b = 0;
            foreach (BeatViewModel beat in Beats)
            {
                beats[b++] = beat.Set;
            }
            return beats;
        }
    }

    internal sealed class BeatViewModel : ViewModelBase
    {
        private bool _set;

        public BeatViewModel(bool set)
        {
            _set = set;
        }

        public bool Set
        {
            get { return _set; }
            set { _set = value; OnPropertyChanged("Set"); }
        }
    }

    internal sealed class DelegateCommand : ICommand
    {
        private readonly Action<object> _execute;

        public DelegateCommand(Action<object> execute)
        {
            _execute = execute;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            _execute(parameter);
        }
    }
}
