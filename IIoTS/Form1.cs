using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace IIoTS
{
    public partial class Form1 : Form
    {
        public S7PROSIMLib.S7ProSim PLC = new S7PROSIMLib.S7ProSim();
        Regex regex = new Regex(@"^[A-Z]\d+\.\d+$");
        
        public Form1()
        {
            InitializeComponent();
            enabled(false);
        }
        //Флаги
        private bool statusVB = false;
        private bool connect = false;
        //Адреса
        private int[] pressure_up_tu = new int[2]; //Давление до
        private int[] pressure_after = new int[2]; //Давление после
        private int[] _operator = new int[2];      //Оператор
        private int[] valve = new int[2];          //Клапан продува

        /// <summary>
        /// Проверка првельно ли указали адреса
        /// </summary>
        /// <returns>true - Успешно, false - Не успешно</returns>
        private bool СheckAddress()
        {
            TextBox[] textBoxes = new TextBox[4] { textBox1, textBox2, textBox3, textBox4 };
            bool flag = true;
            foreach (TextBox TB in textBoxes)
            {
                if (regex.Matches(TB.Text).Count == 0)
                {
                    MessageBox.Show(TB.Text + ". Невірно вказана адреса!");
                    flag = false;
                }
            }
            return flag;
        }
        /// <summary>
        /// Преобразует строку в байт и бит
        /// </summary>
        /// <param name="i">Массив байт и бит</param>
        /// <param name="s">Входная строка</param>
        private void ConvertAddress(int[] i, string s) 
        {
            i[0] = Convert.ToInt32(s.Substring(1, s.IndexOf('.') - 1));
            i[1] = Convert.ToInt32(s.Substring(s.IndexOf('.') + 1));
        }

        /// <summary>
        /// Вкл/Выкл елементов интерфейса
        /// </summary>
        /// <param name="val">true - Вкл, false - Выкл</param>
        private void enabled(bool val)
        {
            RUN_P_button.Enabled = val;
            RUN_button.Enabled = val;
            STOP_button.Enabled = val;
            Valve_button.Enabled = val;
            checkBox4.Enabled = val;
            checkBox5.Enabled = val;
            timer_ReadOutput.Enabled = val;
            textBox1.Enabled = !val;
            textBox2.Enabled = !val;
            textBox3.Enabled = !val;
            textBox4.Enabled = !val;
        }

        /// <summary>
        /// Установка режимов работы PLC 
        /// </summary>
        /// <param name="s">Может быть: RUN, RUN_P, STOP.</param>
        private void SetState(string s)
        {
            PLC.SetState(s);
            label_SPU_State.Text = "CPU State: " + PLC.GetState();
            toolStripStatusLabel1.Text = "Connected to PLCSim: " + PLC.GetState() + "\t";
        }

        /// <summary>
        /// Отображение статуса подключения и адресов
        /// </summary>
        private void GetState()
        {
            label_SPU_State.Text = "CPU State: " + PLC.GetState();
            label_Scan_Mode.Text = "Scan Mode: " + PLC.GetScanMode().ToString();
            toolStripStatusLabel1.Text = "Connected to PLCSim: " + PLC.GetState() + "\t";
            toolStripStatusLabel2.Text = "Inputs: " + textBox1.Text + ", " + textBox2.Text + ", " + textBox3.Text;
            toolStripStatusLabel3.Text = "Outputs: " + textBox4.Text;
        }

        //Кнопка подключения к эмулятору
        private void Connect_button_Click(object sender, EventArgs e)
        {
            if (СheckAddress())
            {
                ConvertAddress(pressure_up_tu, textBox1.Text);
                ConvertAddress(pressure_after, textBox2.Text);
                ConvertAddress(_operator, textBox3.Text);
                ConvertAddress(valve, textBox4.Text);

                if (!connect)
                {
                    PLC.Connect();
                    PLC.SetScanMode(S7PROSIMLib.ScanModeConstants.ContinuousScan);
                    GetState();
                    Connect_button.Text = "Disconnect";
                    enabled(true);
                }
                else
                {
                    PLC.Disconnect();
                    GetState();
                    Connect_button.Text = "Connect";
                    enabled(false);
                }
                connect = !connect;
            }
        }

        //Кнопки управления эмулятором
        private void RUN_P_button_Click(object sender, EventArgs e)
        {
            SetState("RUN_P");
        }

        private void RUN_button_Click(object sender, EventArgs e)
        {
            SetState("RUN");
        }

        private void STOP_button_Click(object sender, EventArgs e)
        {
            SetState("STOP");
        }

        //Управление давлением до филтра
        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            object value = checkBox4.Checked;
            PLC.WriteInputPoint(pressure_up_tu[0], pressure_up_tu[1], ref value);
            if ((bool)value)
            {
                pictureBox2.BackColor = Color.Green;
                label2.Text = "Тиск до фільтру: присутній";
            }
            else if (!(bool)value)
            {
                pictureBox2.BackColor = Color.Red;
                label2.Text = "Тиск до фільтру: присутній";
            }
        }

        //Управление давлением после филтра
        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            object value = checkBox5.Checked;
            PLC.WriteInputPoint(pressure_after[0], pressure_after[1], ref value);
            if ((bool)value)
            {
                pictureBox4.BackColor = Color.Green;
                label3.Text = "Тиск після фільтру: присутній";
            }
            else if (!(bool)value)
            {
                pictureBox4.BackColor = Color.Red;
                label3.Text = "Тиск після фільтру: відсутній";
            }
        }

        //Таймер парсим память
        private void timer_ReadOutput_Tick(object sender, EventArgs e)
        {
            object value = false;
            PLC.ReadOutputPoint(valve[0], valve[1], S7PROSIMLib.PointDataTypeConstants.S7_Bit, ref value);
            if ((bool)value) pictureBox3.BackColor = Color.Green;
            else if (!(bool)value) pictureBox3.BackColor = Color.Red;

            object value2 = false;
            object value3 = false;
            object value4 = false;

            PLC.ReadFlagValue(103, 0, S7PROSIMLib.PointDataTypeConstants.S7_Bit, ref value2);
            PLC.ReadFlagValue(103, 1, S7PROSIMLib.PointDataTypeConstants.S7_Bit, ref value3);
            PLC.ReadFlagValue(103, 2, S7PROSIMLib.PointDataTypeConstants.S7_Bit, ref value4);

            if ((bool)value2)
            {
                pictureBox2.BackColor = Color.Green;
                label2.Text = "Тиск до фільтру: присутній";
            }
            else
            {
                pictureBox2.BackColor = Color.Red;
                label2.Text = "Тиск до фільтру: відсутній";
            }

            if ((bool)value3)
            {
                pictureBox4.BackColor = Color.Green;
                label3.Text = "Тиск після фільтру: присутній";
            }
            else
            {
                pictureBox4.BackColor = Color.Red;
                label3.Text = "Тиск після фільтру: відсутній";
            }

            if ((bool)value4)
            {
                pictureBox3.BackColor = Color.Green;
            }
            else
            {
                pictureBox3.BackColor = Color.Red;
            }

        }

        //Кнопка оператора вкл/выкл
        private void Valve_button_Click(object sender, EventArgs e)
        {
            object value = statusVB;
            PLC.WriteInputPoint(_operator[0], _operator[1], ref value);
            if (statusVB) Valve_button.Text = "On";
            else if (!statusVB) Valve_button.Text = "Off";
            statusVB = !statusVB;
        }
    }
}