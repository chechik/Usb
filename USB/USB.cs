using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Management;
using System.Collections;
using System.Linq;


namespace USBTestApp
{
    public class USBdevices
    {
        private List<DriveInfo> _drivesList;
        private DriveInfo _USBstorage = null;
        private DriveInfo _SDcard = null;
        private ManagementEventWatcher w = null;
        public string _NSFfilename = "head_data.nsf";
        public string _tempDirectory = @"C:\3nt_TempDirectory";

        public event EventHandler<EventArgs> SDcardStatusChanged; //Raised when SD card inserted or removed
        public event EventHandler<EventArgs> USBdokStatusChanged; //Raised when USB DOK inserted or removed

        public DriveInfo USBstorage
        {
            get { return _USBstorage; }
        }

        public DriveInfo SDcard
        {
            get { return _SDcard; }
        }

        public USBdevices()
        {
            ConfigureTempDirectory();
            //Run event initiators
            AddInsertUSBHandler();
            AddRemoveUSBHandler();
            //Initialize current drives list 
            _drivesList = GetConnecetdDrives();

            //Check if removable drives already connected and assign them if required
            if (_drivesList.Count > 0)
            {
                foreach (DriveInfo drive in _drivesList)
                {
                    if (CheckNSFfile(drive.Name))
                    {
                        _SDcard = drive;
                    }
                    else
                    {
                        _USBstorage = drive;
                    }
                }
            }
        }

        private List<DriveInfo> GetConnecetdDrives()
        {
            return DriveInfo.GetDrives().Where((drive) =>
            {
                return (drive.IsReady && drive.DriveType == DriveType.Removable);
            }).ToList();
        }


        private DriveInfo GetInsertedDrive()
        {
            List<DriveInfo> newDrivesList = GetConnecetdDrives();

            //Excepted behavior - one new USB drive is inserted
            if (newDrivesList.Count - _drivesList.Count == 1)
            {
                var newDrive = newDrivesList.Where((drive) =>
                {
                    bool exists = _drivesList.Exists((oldDrive) =>
                    {
                        return oldDrive.Name == drive.Name;
                    });
                    return exists ? false : true;
                }).First();
                _drivesList = newDrivesList;
                return newDrive;
            }
            throw new InvalidOperationException("Unexpected  GetInsertedDrive() method behavior.");
        }

        ///<summary>
        ///Check if 3intTempDirectory exists on disk C:
        ///and creates it if it isn't
        ///</summary>
        ///<remarks>
        ///Runs once at initiallization of USBinfo Instance
        ///</remarks>
        private void ConfigureTempDirectory()
        {
            try
            {
                if (!Directory.Exists(_tempDirectory))
                    Directory.CreateDirectory(_tempDirectory);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(string.Format("Can not create directory {0}", _tempDirectory));
            }
        }

        private bool CheckNSFfile(string path)
        {
            return File.Exists(@path + _NSFfilename);
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
                q.WithinInterval = new TimeSpan(0, 0, 2);
                q.Condition = "TargetInstance ISA 'Win32_DiskPartition'";
                w = new ManagementEventWatcher(scope, q);
                w.EventArrived += UpdateDrivesOnRemoval;
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
                q.WithinInterval = new TimeSpan(0, 0, 2);
                q.Condition = "TargetInstance ISA 'Win32_DiskPartition'";
                w = new ManagementEventWatcher(scope, q);
                w.EventArrived += UpdateDrivesOnInsertion;
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
        private void UpdateDrivesOnInsertion(object sender, EventArgs e)
        {
            try
            {
                var insertedDrive = GetInsertedDrive();
                if (_SDcard == null && CheckNSFfile(insertedDrive.Name))
                {
                    _SDcard = insertedDrive;
                    SDcardStatusChanged(this, new EventArgs());
                }
                else if (_USBstorage == null)
                {
                    _USBstorage = insertedDrive;
                    USBdokStatusChanged(this, new EventArgs());
                }
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void UpdateDrivesOnRemoval(object sender, EventArgs e)
        {
            try
            {
                List<DriveInfo> newDrivesList = GetConnecetdDrives();
                if (_SDcard != null && !_SDcard.IsReady)
                {
                    _SDcard = null;
                    SDcardStatusChanged(this, new EventArgs());
                }

                if (_USBstorage != null && !_USBstorage.IsReady)
                {
                    _USBstorage = null;
                    USBdokStatusChanged(this, new EventArgs());
                }
                _drivesList = newDrivesList;
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}