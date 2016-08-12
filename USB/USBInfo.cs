using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Management;
using System.Collections;
using System.Linq;


namespace USB
{
    public class USBInfo
    {
        private List<DriveInfo> _oldDrivesList;
        private DriveInfo _newDrive = null;
        private ManagementEventWatcher w = null;
        public DriveInfo NewDrive
        {
            get { return _newDrive; }
            set{}
        }



        public USBInfo()
        {
            AddInsertUSBHandler();
            AddRemoveUSBHandler();
            _oldDrivesList = GetConnecetdDrives();
            NewDrive = _newDrive;
        }
        private void GetNewDrive()
        {
            List<DriveInfo> newDrivesList = GetConnecetdDrives();

            if (newDrivesList.Count == 0)
            {
                Console.WriteLine("No removable drives connected");
                _oldDrivesList=newDrivesList;
                _newDrive = null;
            }
            else if (newDrivesList.Count - _oldDrivesList.Count == 1)
            {
                _oldDrivesList=newDrivesList;
                _newDrive=newDrivesList.First();
 
            }
            else
            {
                foreach (DriveInfo drive in _oldDrivesList)
                    if (newDrivesList.Contains(drive))
                        newDrivesList.Remove(drive);
            }    
            



            //if(_oldDrivesList.Count > 0)
            //    return GetConnecetdDrives().Where((drive) =>
            //    {
            //        bool exists = _oldDrivesList.Exists((oldDrive) =>
            //        {
            //            return oldDrive.Name == drive.Name;
            //        });
            //        return exists ? false : true;
            //    }).First();
            //return null;
        }
        private List<DriveInfo> GetConnecetdDrives() {
            return DriveInfo.GetDrives().Where((drive) => {
                return (drive.IsReady && drive.DriveType == DriveType.Removable && _oldDrivesList.Exists(f =>  _oldDrivesList.Count>0 && f.VolumeLabel == drive.VolumeLabel));
            }).ToList();

        }
        private void AddRemoveUSBHandler()
        {
            WqlEventQuery q;
            ManagementScope scope = new ManagementScope("root\\CIMV2");
            scope.Options.EnablePrivileges = true;
            try
            {
                q = new WqlEventQuery();
                q.EventClassName = "__InstanceDeletionEvent";
                q.WithinInterval = new TimeSpan(0, 0, 3);
                q.Condition = "TargetInstance ISA 'Win32_USBControllerdevice'";
                w = new ManagementEventWatcher(scope, q);
                w.EventArrived += USBRemoved;

                w.Start();
            }
            catch (Exception e)
            {


                Console.WriteLine(e.Message);
                if (w != null)
                {
                    w.Stop();

                }
            }

        }
        private void AddInsertUSBHandler()
        {

            WqlEventQuery q;
            ManagementScope scope = new ManagementScope("root\\CIMV2");
            scope.Options.EnablePrivileges = true;

            try
            {

                q = new WqlEventQuery();
                q.EventClassName = "__InstanceCreationEvent";
                q.WithinInterval = new TimeSpan(0, 0, 3);
                q.Condition = "TargetInstance ISA 'Win32_USBControllerdevice'";
                w = new ManagementEventWatcher(scope, q);
                w.EventArrived += USBInserted;

                w.Start();
            }
            catch (Exception e)
            {

                Console.WriteLine(e.Message);
                if (w != null)
                {
                    w.Stop();

                }
            }

        }
        private void USBInserted(object sender, EventArgs e)
        {
            Console.WriteLine("A USB device inserted");
            GetNewDrive(); 
            if ( _newDrive != null)
                Console.WriteLine("Added USB drive: {0}: {1}", _newDrive.VolumeLabel, _newDrive.RootDirectory);
                //_oldDrivesList = GetConnecetdDrives();
            if (_newDrive != null)
            {
                Console.WriteLine("\nUsb Drives list:\n");
                foreach (DriveInfo drive in _oldDrivesList)
                {
                    Console.WriteLine("{0}: {1}\n", drive.VolumeLabel, drive.RootDirectory);
                }
            }
                
        }
        private void USBRemoved(object sender, EventArgs e)
        {
            Console.WriteLine("A USB device removed");

            _oldDrivesList = GetConnecetdDrives();
        }
    }
}