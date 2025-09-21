using System.Windows.Controls;
/*
Copyright © from 2022 to present, UNKNOWN STRYKER. All Rights Reserved.
Licensed under the AGPLv3 License. You may not use this file except in compliance with the License.
*/




namespace Installer
{
    public enum OpMode
    {
        Install,
        Uninstall
    };


    /// <summary>
    /// Interaction logic for FinalPage.xaml
    /// </summary>
    public partial class FinalPage : Page
    {
        public FinalPage()
        {
            InitializeComponent();
        }


        public void SetOpMode(OpMode mode)
        {
            if (mode == OpMode.Install)
            {
                this.FinalMessage.Text = "Successfully Installed the Frogman Engine GDK...!";
                return;
            }

            this.FinalMessage.Text = "Successfully Uninstalled the Frogman Engine GDK...!";
        }
    };
}
