using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace DeleteStructures_sa
{
    /// <summary>
    /// Interaction logic for DeleteForm.xaml
    /// </summary>
    public partial class DeleteForm : UserControl
    {
        private readonly StructureSet _structureSet;

        public DeleteForm()
        {
            InitializeComponent();
            Height = double.NaN; Width = double.NaN;
            MinHeight = 300;
            MinWidth = 400;

            #region Banner

            Assembly assembly = Assembly.GetExecutingAssembly();
            string[] resources = assembly.GetManifestResourceNames();

            string imgName = resources.FirstOrDefault(x => x.Contains("banner"));
            PngBitmapDecoder png = new PngBitmapDecoder(
                assembly.GetManifestResourceStream(imgName), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default
                );
            banner.Source = png.Frames[0];

            #endregion

            lbStructures.SelectionChanged += LbStructures_SelectionChanged;
            btnDelete.IsEnabled = false;
        }
        public DeleteForm(ScriptContext context) : this()
        {
            _structureSet = context.StructureSet;
            txtPatient.Text = $"{context.Patient.Id} - {_structureSet.Id}";
            ScanStructureSet();
        }

        private void BtnAll_Click(object sender, RoutedEventArgs e)
        {
            lbStructures.SelectAll();
        }
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Window window = Window.GetWindow(this);
            window.Close();
        }
        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            WorkClass.Items = (IEnumerable<object>)lbStructures.SelectedItems;
            WorkClass.StructureSet = _structureSet;
            WorkClass.DoWork();
            ScanStructureSet();
        }
        private void BtnNone_Click(object sender, RoutedEventArgs e)
        {
            lbStructures.SelectedItems.Clear();
        }
        private void LbStructures_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbStructures.SelectedItems.Count > 0)
            {
                btnDelete.IsEnabled = true;
            }
            else
            {
                btnDelete.IsEnabled = false;
            }
        }

        private void ScanStructureSet()
        {
            List<string> strIDs = new List<string>();
            foreach (Structure s in _structureSet.Structures)
            {
                List<StructureApprovalHistoryEntry> approvals = new List<StructureApprovalHistoryEntry>(s.ApprovalHistory);
                approvals.Sort(new ApprovalHistoryComparer());
                if (approvals.Last().ApprovalStatus == StructureApprovalStatus.Approved)
                {
                    continue;
                }
                if (!_structureSet.CanRemoveStructure(s))
                {
                    continue;
                }
                strIDs.Add(s.Id);
            }
            strIDs.Sort();
            lbStructures.SelectedItems.Clear();
            lbStructures.ItemsSource = strIDs;
        }
    }
}
