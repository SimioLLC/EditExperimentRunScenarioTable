﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace EditExperimentRunScenarioTable
{
    public partial class editExperimentRunScenarioTable : Form
    {
        private Int32 experimentRunId = -1;
        private String oldValue = String.Empty;
        public editExperimentRunScenarioTable()
        {
            InitializeComponent();
            experimentRunId = getExperimentRunId();
            var xmlDoc = SimioPortalWebAPIHelper.getModelTableSchema(experimentRunId);
            var dataNodes = xmlDoc.SelectSingleNode("data");
            foreach (XmlNode itemNodes in dataNodes)
            {
                XmlNodeList tableNameNode = ((XmlElement)itemNodes).GetElementsByTagName("TableName");
                tableComboBox.Items.Add(tableNameNode[0].InnerText);
            }
        }


        private void loadTableDataGridView()
        {
            AddStatusMessage("Get Table Data");
            var xmlDOC = SimioPortalWebAPIHelper.getExperimentRunTableRowData(experimentRunId, tableComboBox.Text);

            AddStatusMessage("Transforming Table Data");
            string cleanStylesheet = stylesheetTextBox.Text;
            cleanStylesheet = cleanStylesheet.Replace("\n", String.Empty);
            cleanStylesheet = cleanStylesheet.Replace("\r", String.Empty);
            cleanStylesheet = cleanStylesheet.Replace("\t", String.Empty);

            DataSet ds = XMLTransformationUtils.GetDataSetFromTransform(cleanStylesheet, xmlDOC.OuterXml);
            tableDataGridView.DataSource = ds.Tables[0];
            tableDataGridView.CellBeginEdit += TableDataGridView_CellBeginEdit;
            tableDataGridView.CellEndEdit += TableDataGridView_CellEndEdit;
        }

        private void TableDataGridView_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            oldValue = tableDataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
        }

        private void TableDataGridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {      
            try
            {
                AddStatusMessage("Edit Row");
                if (tableDataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString() != oldValue)
                {
                    Int32 rowIndex = tableDataGridView.CurrentRow.Index;
                    SimioPortalWebAPIHelper.setExperimentRunScenarioTableRows(experimentRunId, rowIndex, tableComboBox.Text, tableDataGridView.Columns[e.ColumnIndex].Name, tableDataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString());
                }
            }
            catch (Exception ex)
            {                
                MessageBox.Show(ex.Message);
                tableDataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = oldValue;                
            }
        }

        private Int32 getExperimentRunId()
        {            
            if (Uri.TryCreate(SimioPortalWebAPIHelper.Url, UriKind.Absolute, out SimioPortalWebAPIHelper.Uri) == false)
            {
                throw new Exception("URL Setting in an invalid format");
            }

            if (String.IsNullOrWhiteSpace(SimioPortalWebAPIHelper.AuthenticationType) == false && SimioPortalWebAPIHelper.AuthenticationType.ToLower() != "none")
            {
                AddStatusMessage("Set Credentials");
                SimioPortalWebAPIHelper.setCredentials();
            }

            AddStatusMessage("Obtain Bearer Token");
            SimioPortalWebAPIHelper.obtainBearerToken();
            AddStatusMessage("Find Experiment Ids");
            Int32[] returnInt32 = SimioPortalWebAPIHelper.findExperimentIds();
            Int32 experimentRunId = returnInt32[0];
            Int32 experimentId = returnInt32[1];
            AddStatusMessage("ExperimentRunId:" + experimentRunId.ToString() + "|ExperimentId:" + experimentId.ToString());
            return experimentRunId;
        }

        private void AddStatusMessage(string message)
        {
            if (statusTextBox.Text.Length > 0) statusTextBox.Text += Environment.NewLine;
            statusTextBox.Text += message;
        }

        private void insertButton_Click(object sender, EventArgs e)
        {
            try
            {
                AddStatusMessage("Insert Row");
                Int32 rowIndex = tableDataGridView.CurrentRow.Index;
                SimioPortalWebAPIHelper.insertExperimentRunScenarioTableRows(experimentRunId, rowIndex, tableComboBox.Text);
                loadTableDataGridView();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void deleteButton_Click(object sender, EventArgs e)
        {
            try
            {
                AddStatusMessage("Delete Row");
                Int32 rowIndex = tableDataGridView.CurrentRow.Index;
                SimioPortalWebAPIHelper.deleteExperimentRunScenarioTableRows(experimentRunId, rowIndex, tableComboBox.Text);
                loadTableDataGridView();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void tableComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                statusTextBox.Text = String.Empty;
                loadTableDataGridView();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
