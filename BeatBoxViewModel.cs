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
    internal sealed class BeatBoxViewModel
    {
        private readonly ICollectionView _devices;
        private readonly ICommand _playCommand;
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
            _playCommand = new DelegateCommand(Play);
            _windowHandle = windowHandle;
        }

        public ICollectionView Devices
        {
            get { return _devices; }
        }

        public ICommand PlayCommand
        {
            get { return _playCommand; }
        }

        private void Play(object parameter)
        {
            DeviceViewModel currentDevice = (DeviceViewModel)_devices.CurrentItem;
            if (currentDevice != null)
            {
                Device playbackDevice = new Device(currentDevice.DriverGuid);
                playbackDevice.SetCooperativeLevel(_windowHandle, CooperativeLevel.Normal);

                Track track1 = new Track(WaveData.FromFile(@"d:\dev\beatbox\samples\bass.wav"), new bool[] { true, false, false, true, true, false, false, false, true, false, false, true, true, false, false, false });
                Track track2 = new Track(WaveData.FromFile(@"d:\dev\beatbox\samples\open.wav"), new bool[] { false, false, false, false, false, false, false, true, false, false, false, false, false, false, false, true });
                Track track3 = new Track(WaveData.FromFile(@"d:\dev\beatbox\samples\snare.wav"), new bool[] { false, false, true, false, false, false, true, false, false, false, true, false, false, false, true, false });
                Track track4 = new Track(WaveData.FromFile(@"d:\dev\beatbox\samples\closed.wav"), new bool[] { true, true, false, true, true, true, false, false, true, true, false, true, true, true, false, false });

                BeatBoxMixer mixer = new BeatBoxMixer(120, new Track[] { track1, track2, track3, track4 });

                //Track track1 = new Track(WaveData.FromFile(@"d:\dev\beatbox\samples\bass.wav"), new bool[] { true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true });
                //BeatBoxMixer mixer = new BeatBoxMixer(240, new Track[] { track1 });

                StreamPlayer player = new StreamPlayer(playbackDevice, track1.Format, mixer);
                player.Play();
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
