// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using MetriCam2;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Metrilus.Util;

namespace MetriCam2.Controls
{
    public partial class CameraSettingsControl : UserControl
    {
        #region Types
        /// <summary>
        /// Alphabetical sorting, while Auo*-parameters are placed before their base parameter.
        /// </summary>
        internal class ParamDescComparer : IComparer<ParamDesc>
        {
            /// <summary>
            /// Alphabetical sorting, while Auo*-parameters are placed before their base parameter.
            /// </summary>
            public int Compare(ParamDesc x, ParamDesc y)
            {
                if (!x.IsAutoParameter() && !y.IsAutoParameter())
                {
                    return x.Name.CompareTo(y.Name);
                }

                // Handle Auto*-parameters:
                // If at least one parameter is an Auto*-parameter, then just compare the base names.

                string xBaseName = x.GetBaseParameterName();
                string yBaseName = y.GetBaseParameterName();

                int c = xBaseName.CompareTo(yBaseName);

                // If the base names are the same, then one must be the Auto*- and the other the base parameter.
                // Thus, simply compare the original names.
                if (0 == c)
                {
                    c = x.Name.CompareTo(y.Name);
                }

                return c;
            }
        }

        /// <summary>
        /// ButtonEventArgs
        /// </summary>
        public class ButtonEventArgs : EventArgs
        {
            private MethodInfo info;
            /// <summary>
            /// MethodInfo.
            /// </summary>
            public MethodInfo Info
            {
                get { return info; }
                set { info = value; }
            }
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="info"></param>
            public ButtonEventArgs(MethodInfo info)
            {
                this.info = info;
            }
        }
        #endregion

        #region Private Fields
        private const int COL_PARAM_NAME = 0;
        private const int COL_PARAM_VAL = 1;
        private const int COL_PARAM_UNIT = 2;
        private static readonly string VALUE_SUFFIX = "_value";

        private Camera cam;
        #endregion

        #region Public Properties
        public Camera Camera
        {
            get { return cam; }
            set
            {
                if (null == value)
                {
                    return;
                }

                if (null != cam)
                {
                    cam.OnConnected -= InitConfigurationParameters;
                    cam.OnDisconnected -= InitConfigurationParameters;
                }

                cam = value;

                // Update icon
                //   use cam.CameraIcon
                // Update list of channels
                //   use cam.Channels;
                // Update child controls.
                InitConfigurationParameters(cam);

                cam.OnConnected += InitConfigurationParameters;
                cam.OnDisconnected += InitConfigurationParameters;
            }
        }

        public bool ContainsOneOrMoreWritableParameters { get; set; }

        public List<string> VisibleParameters { get; set; }
        public Color TextColor { get; set; }
        public Font HeadingFont { get; set; }
        public Font LabelFont { get; set; }
        #endregion

        #region Constructor
        public CameraSettingsControl()
        {
            InitializeComponent();
            LabelFont = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            HeadingFont = new Font(LabelFont.FontFamily, LabelFont.Size, FontStyle.Bold | FontStyle.Underline);
            TextColor = Color.Black;
        }
        #endregion

        #region Private Methods
        private string SeperateString(string value)
        {
            return Regex.Replace(value, "((?<=[a-z])[A-Z]|[A-Z](?=[a-z]))", " $1");
        }

        private void InitConfigurationParameters(Camera cam)
        {
            this.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();

            // reset view
            tableLayoutPanel1.Controls.Clear();
            tableLayoutPanel1.RowStyles.Clear();

            int currentRow = 0;

            AddHeadingRow(currentRow);

            List<ParamDesc> parameters = new List<ParamDesc>();
            List<ParamDesc> allParameters = cam.GetParameters();

            if (VisibleParameters != null)
            {
                foreach (var item in allParameters)
                {
                    if (VisibleParameters.Contains(item.Name))
                    {
                        parameters.Add(item);
                    }
                }
            }
            else
            {
                parameters = allParameters;
            }

            parameters.Sort(new ParamDescComparer());

            ContainsOneOrMoreWritableParameters = false;
            int contentHeight = HeadingFont.Height * 2;
            foreach (var paramDesc in parameters)
            {
                currentRow++;
                tableLayoutPanel1.RowCount = currentRow + 1;

                int rowHeight = LabelFont.Height * 2;
                if (paramDesc is MultiFileParamDesc)
                {
                    rowHeight = MultiFileSelector.StandardHeight + 8;
                }
                tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, rowHeight));
                contentHeight += rowHeight;

                tableLayoutPanel1.Controls.Add(CreateNameLabel(paramDesc), COL_PARAM_NAME, currentRow);

                // Build a suitable control for the current parameter
                // Parameter with a value range
                if (paramDesc is IRangeParamDesc)
                {
                    if (paramDesc is RangeParamDesc<int>)
                    {
                        Slider scrollbarValue = CreateSlider((RangeParamDesc<int>)paramDesc, currentRow);
                        tableLayoutPanel1.Controls.Add(scrollbarValue, COL_PARAM_VAL, currentRow);
                        Label unit = CreateUnitLabel(paramDesc);
                        tableLayoutPanel1.Controls.Add(unit, COL_PARAM_UNIT, currentRow);
                        if (scrollbarValue.Enabled)
                        {
                            ContainsOneOrMoreWritableParameters = true;
                        }
                        scrollbarValue.ValueChanged += (sender, e) =>
                        {
                            string parameterValue = scrollbarValue.Value.ToString(CultureInfo.InvariantCulture);
                            Camera.SetParameter(paramDesc.Name, parameterValue);
                        };
                    }
                    else if (paramDesc is RangeParamDesc<float>)
                    {
                        NumericUpDown upDownValue = CreateNumericUpDown((RangeParamDesc<float>)paramDesc, currentRow);
                        tableLayoutPanel1.Controls.Add(upDownValue, COL_PARAM_VAL, currentRow);
                        Label unit = CreateUnitLabel(paramDesc);
                        tableLayoutPanel1.Controls.Add(unit, COL_PARAM_UNIT, currentRow);
                        if (upDownValue.Enabled)
                        {
                            ContainsOneOrMoreWritableParameters = true;
                        }

                        upDownValue.ValueChanged += (sender, e) =>
                        {
                            string parameterValue = upDownValue.Value.ToString(CultureInfo.InvariantCulture);
                            Camera.SetParameter(paramDesc.Name, parameterValue);
                        };
                    }
                    else
                    {
                        TextBox textBoxValue = CreateTextBox(paramDesc, currentRow);
                        tableLayoutPanel1.Controls.Add(textBoxValue, COL_PARAM_VAL, currentRow);
                        Label unit = CreateUnitLabel(paramDesc);
                        tableLayoutPanel1.Controls.Add(unit, COL_PARAM_UNIT, currentRow);
                        if (textBoxValue.Enabled)
                        {
                            ContainsOneOrMoreWritableParameters = true;
                        }
                    }

                    continue;
                }

                // Parameter with a list of values
                if (paramDesc is IListParamDesc)
                {
                    ComboBox comboBoxValue = CreateComboBox(paramDesc as IListParamDesc, currentRow);
                    tableLayoutPanel1.Controls.Add(comboBoxValue, COL_PARAM_VAL, currentRow);
                    Label unit = CreateUnitLabel(paramDesc);
                    tableLayoutPanel1.Controls.Add(unit, COL_PARAM_UNIT, currentRow);
                    if (comboBoxValue.Enabled)
                    {
                        ContainsOneOrMoreWritableParameters = true;
                    }

                    if(paramDesc is ListParamDesc<Point2i>
                    || paramDesc is ListParamDesc<int>)
                    {
                        comboBoxValue.SelectedValueChanged += (sender, e) =>
                        {
                            object parameterValue;

                            if (paramDesc is ListParamDesc<Point2i>)
                            {
                                parameterValue = TypeConversion.ResolutionToPoint2i(comboBoxValue.SelectedItem as string);
                            }
                            else
                            {
                                parameterValue = int.Parse(comboBoxValue.SelectedItem as string);
                            }

                            Camera.SetParameter(paramDesc.Name, parameterValue);
                        };
                    }

                    continue;
                }

                if (paramDesc is MultiFileParamDesc)
                {
                    MultiFileSelector fileSelector = CreateMultiFileSelector(paramDesc as MultiFileParamDesc, currentRow);
                    tableLayoutPanel1.Controls.Add(fileSelector, COL_PARAM_VAL, currentRow);
                    Label unit = CreateUnitLabel(paramDesc);
                    tableLayoutPanel1.Controls.Add(unit, COL_PARAM_UNIT, currentRow);
                    if (fileSelector.Enabled)
                    {
                        ContainsOneOrMoreWritableParameters = true;
                    }

                    continue;
                }

                // Parameter of type bool
                if (paramDesc is ParamDesc<bool>)
                {
                    // TODO: build a checkbox
                    CheckBox checkBoxValue = CreateCheckBox(paramDesc as ParamDesc<bool>, currentRow);
                    tableLayoutPanel1.Controls.Add(checkBoxValue, COL_PARAM_VAL, currentRow);
                    Label unit = CreateUnitLabel(paramDesc);
                    tableLayoutPanel1.Controls.Add(unit, COL_PARAM_UNIT, currentRow);
                    if (checkBoxValue.Enabled)
                    {
                        ContainsOneOrMoreWritableParameters = true;
                    }

                    checkBoxValue.CheckStateChanged += (sender, e) =>
                    {
                        string parameterValue = checkBoxValue.Checked.ToString(CultureInfo.InvariantCulture);
                        Dictionary<string, object> keyValues = new Dictionary<string, object>();
                        keyValues.Add(paramDesc.Name, parameterValue);
                        Camera.SetParameters(keyValues);
                    };

                    continue;
                }

                // Parameter with a primitive value (e.g. int, string, float, ...)
                if (paramDesc is ParamDesc)
                {
                    // build a text box
                    TextBox textBoxValue = CreateTextBox(paramDesc, currentRow);
                    tableLayoutPanel1.Controls.Add(textBoxValue, COL_PARAM_VAL, currentRow);
                    Label unit = CreateUnitLabel(paramDesc);
                    tableLayoutPanel1.Controls.Add(unit, COL_PARAM_UNIT, currentRow);

                    if (textBoxValue.Enabled)
                    {
                        ContainsOneOrMoreWritableParameters = true;
                    }

                    continue;
                }

                tableLayoutPanel1.ResumeLayout();
                this.ResumeLayout();
                throw new TypeAccessException("No suitable control found for this parameter type.");
            }
            tableLayoutPanel1.RowCount++;

            // Update control size
            this.Height = contentHeight + 2; // add some space for the borders. 2px is the minimum on my (Hannes) Win7 PC
            this.MinimumSize = new Size(this.Width, this.Height);

            tableLayoutPanel1.ResumeLayout();
            this.ResumeLayout();
        }

        private void AddHeadingRow(int currentRow)
        {
            tableLayoutPanel1.RowCount = currentRow + 1;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, HeadingFont.Height * 2f));

            Label labelHeadParam = new Label();
            labelHeadParam.Font = HeadingFont;
            labelHeadParam.Text = "Parameter";
            labelHeadParam.ForeColor = TextColor;
            labelHeadParam.AutoSize = true;
            labelHeadParam.Anchor = AnchorStyles.Right;
            tableLayoutPanel1.Controls.Add(labelHeadParam, COL_PARAM_NAME, currentRow);

            Label labelHeadValue = new Label();
            labelHeadValue.Font = HeadingFont;
            labelHeadValue.Text = "Value";
            labelHeadValue.ForeColor = TextColor;
            labelHeadValue.AutoSize = true;
            labelHeadValue.Anchor = AnchorStyles.Left;
            tableLayoutPanel1.Controls.Add(labelHeadValue, COL_PARAM_VAL, currentRow);

            Label labelHeadUnit = new Label();
            labelHeadUnit.Font = HeadingFont;
            labelHeadUnit.Text = "Unit";
            labelHeadUnit.ForeColor = TextColor;
            labelHeadUnit.AutoSize = true;
            labelHeadUnit.Anchor = AnchorStyles.Left;
            tableLayoutPanel1.Controls.Add(labelHeadUnit, COL_PARAM_UNIT, currentRow);
        }

        private Label CreateNameLabel(ParamDesc paramDesc)
        {
            Label labelName = new Label();
            labelName.Font = LabelFont;
            labelName.Name = paramDesc.Name + "_label";
            labelName.Text = SeperateString(paramDesc.DisplayName) + ":";
            labelName.ForeColor = TextColor;
            labelName.AutoSize = true;
            labelName.Anchor = AnchorStyles.Right;
            labelName.Enabled = paramDesc.IsWritable;

            return labelName;
        }

        private Label CreateUnitLabel(ParamDesc paramDesc)
        {
            string unitText = "";
            if (null != paramDesc && null != paramDesc.Unit)
            {
                unitText = paramDesc.Unit.ToString(CultureInfo.InvariantCulture);
            }

            Label labelUnit = new Label();
            labelUnit.Font = LabelFont;
            labelUnit.Name = paramDesc.Name + "_unit";
            labelUnit.Text = unitText;
            labelUnit.ForeColor = TextColor;
            labelUnit.AutoSize = true;
            labelUnit.Anchor = AnchorStyles.Left;
            labelUnit.Enabled = paramDesc.IsWritable;

            return labelUnit;
        }

        private void CreateWarningLabel(ParamDesc paramDesc, Exception ex, int currentRow)
        {
            //WarningControl wc = new WarningControl();
            Label warningLabel = new Label();
            warningLabel.Name = paramDesc.Name + "_warn";
            warningLabel.Size = new System.Drawing.Size(20, 20);
            warningLabel.Visible = true;
            warningLabel.Text = "!";
            warningLabel.BackColor = Color.Orange;
            if (ex.InnerException != null)
            {
                warningLabel.Text = ex.InnerException.Message;
            }

            tableLayoutPanel1.Controls.Add(warningLabel, 0, currentRow);
        }

        private CheckBox CreateCheckBox(ParamDesc<bool> paramDesc, int currentRow)
        {
            CheckBox checkBoxValue = new CheckBox();
            checkBoxValue.Name = paramDesc.Name + VALUE_SUFFIX;
            checkBoxValue.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            checkBoxValue.Height = (int)(LabelFont.Height * 2f);
            checkBoxValue.Text = paramDesc.Description;

            if (paramDesc.IsReadable)
            {
                try
                {
                    checkBoxValue.Checked = (bool)paramDesc.Value;
                }
                catch (Exception ex)
                {
                    CreateWarningLabel(paramDesc, ex, currentRow);
                }
            }
            if (!paramDesc.IsWritable)
            {
                checkBoxValue.Enabled = false;
            }

            return checkBoxValue;
        }

        private ComboBox CreateComboBox(IListParamDesc listParamDesc, int currentRow)
        {
            ParamDesc paramDesc = (ParamDesc)listParamDesc;
            ComboBox comboBoxValue = new ComboBox();
            comboBoxValue.Name = paramDesc.Name + VALUE_SUFFIX;
            comboBoxValue.Height = LabelFont.Height;
            comboBoxValue.Font = LabelFont;
            comboBoxValue.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            if (paramDesc.IsReadable)
            {
                for (int i = 0; i < listParamDesc.AllowedValues.Count; i++)
                {
                    var item = listParamDesc.AllowedValues[i];
                    comboBoxValue.Items.Add(item.ToString(CultureInfo.InvariantCulture));
                    if (paramDesc.Type.IsEnum)
                    {
                        if (paramDesc.Value.ToString() == item)
                        {
                            comboBoxValue.SelectedIndex = i;
                        }
                    }
                    else
                    {
                        object tmpVal;

                        if (paramDesc is ListParamDesc<Point2i>)
                        {
                            tmpVal = TypeConversion.ResolutionToPoint2i(item);
                        }
                        else
                        {
                            tmpVal = Convert.ChangeType(item, paramDesc.Type, CultureInfo.InvariantCulture);
                        }

                        if (null != tmpVal && paramDesc.Value.Equals(tmpVal))
                        {
                            comboBoxValue.SelectedIndex = i;
                        }
                    }
                }
            }
            if (!paramDesc.IsWritable)
            {
                comboBoxValue.Enabled = false;
            }

            return comboBoxValue;
        }

        private MultiFileSelector CreateMultiFileSelector(MultiFileParamDesc multiFileParamDesc, int currentRow)
        {
            MultiFileSelector fileSelector = new MultiFileSelector(multiFileParamDesc);
            fileSelector.Name = multiFileParamDesc.Name + VALUE_SUFFIX;
            fileSelector.Font = LabelFont;
            fileSelector.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            if (!multiFileParamDesc.IsWritable)
            {
                fileSelector.Enabled = false;
            }

            return fileSelector;
        }

        private NumericUpDown CreateNumericUpDown(RangeParamDesc<float> paramDesc, int currentRow)
        {
            NumericUpDown numericUpDownValue = new NumericUpDown();
            numericUpDownValue.Name = paramDesc.Name + VALUE_SUFFIX;
            numericUpDownValue.Height = LabelFont.Height;
            numericUpDownValue.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            if (paramDesc.IsReadable)
            {
                try
                {
                    numericUpDownValue.Minimum = (decimal)paramDesc.Min;
                    numericUpDownValue.Maximum = (decimal)paramDesc.Max;
                    numericUpDownValue.DecimalPlaces = 2;
                    numericUpDownValue.Value = (decimal)paramDesc.Value;
                }
                catch (Exception ex)
                {
                    CreateWarningLabel(paramDesc, ex, currentRow);
                }
            }
            if (!paramDesc.IsWritable)
            {
                numericUpDownValue.Enabled = false;
            }

            return numericUpDownValue;
        }

        private Slider CreateSlider(RangeParamDesc<int> paramDesc, int currentRow)
        {
            Slider slider = new Slider();
            slider.Name = paramDesc.Name + VALUE_SUFFIX;
            slider.Font = LabelFont;
            slider.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            slider.Height = LabelFont.Height;
            if (paramDesc.IsReadable)
            {
                try
                {
                    slider.Minimum = (int)paramDesc.Min;
                    slider.Maximum = (int)paramDesc.Max;
                    slider.Value = (int)paramDesc.Value;
                }
                catch (Exception ex)
                {
                    CreateWarningLabel(paramDesc, ex, currentRow);
                }
            }
            if (!paramDesc.IsWritable)
            {
                slider.Enabled = false;
            }

            return slider;
        }

        private TextBox CreateTextBox(ParamDesc paramDesc, int currentRow)
        {
            TextBox textBoxValue = new TextBox();
            textBoxValue.Name = paramDesc.Name + VALUE_SUFFIX;
            textBoxValue.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            textBoxValue.Height = LabelFont.Height;
            textBoxValue.Font = LabelFont;
            if (paramDesc.IsReadable)
            {
                try
                {
                    textBoxValue.Text = paramDesc.Value.ToString();
                }
                catch (Exception ex)
                {
                    CreateWarningLabel(paramDesc, ex, currentRow);
                }
            }
            if (!paramDesc.IsWritable)
            {
                textBoxValue.Enabled = false;
            }

            return textBoxValue;
        }
        #endregion
    }
}