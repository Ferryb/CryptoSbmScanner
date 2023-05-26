﻿using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Signal;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CryptoSbmScanner
{
    public partial class AskSymbolDialog : Form
    {
        public AskSymbolDialog()
        {
            InitializeComponent();

            EditSymbol.Text = GlobalData.Settings.BackTest.BackTestSymbol;

            // De intervallen in de combox zetten (default=1h)
            EditInterval.Items.Clear();
            foreach (CryptoInterval interval in GlobalData.IntervalList)
                EditInterval.Items.Add(interval.Name);
            EditInterval.MaxDropDownItems = EditInterval.Items.Count;
            EditInterval.SelectedIndex = EditInterval.Items.IndexOf(GlobalData.Settings.BackTest.BackTestInterval);
            if (EditInterval.SelectedIndex < 0)
                EditInterval.SelectedIndex = 0;

            EditAlgoritm.Items.Clear();
            foreach (AlgorithmDefinition definition in TradingConfig.AlgorithmDefinitionList)
                EditAlgoritm.Items.Add(definition.Name);
            //foreach (SignalLongStrategy strategy in Enum.GetValues(typeof(SignalLongStrategy)))
                //EditAlgoritm.Items.Add(SignalHelper.GetSignalAlgorithmText(strategy));
            EditAlgoritm.MaxDropDownItems = EditAlgoritm.Items.Count;
            EditAlgoritm.SelectedIndex = (int)GlobalData.Settings.BackTest.BackTestAlgoritm;
            if (EditAlgoritm.SelectedIndex < 0)
                EditAlgoritm.SelectedIndex = 0;

            EditTime.Text = GlobalData.Settings.BackTest.BackTestTime.ToLocalTime().ToString("dd-MM-yyyy HH:mm");
        }

        private void ButtonOk_Click(object sender, EventArgs e)
        {
            DateTime date = DateTime.ParseExact(EditTime.Text.Trim(), "dd-MM-yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture);

            GlobalData.Settings.BackTest.BackTestSymbol = EditSymbol.Text.Trim();
            GlobalData.Settings.BackTest.BackTestInterval = EditInterval.Text.Trim();
            GlobalData.Settings.BackTest.BackTestTime = date.ToUniversalTime();
            //TODO long/short?
            GlobalData.Settings.BackTest.BackTestAlgoritm = (SignalStrategy)EditAlgoritm.SelectedIndex;

            DialogResult = DialogResult.OK;
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }
    }
}