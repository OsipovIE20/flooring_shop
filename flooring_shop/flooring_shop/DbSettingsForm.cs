﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using MySql.Data.MySqlClient;
using System.IO;

namespace flooring_shop
{
    public partial class DbSettingsForm : Form
    {
        public DbSettingsForm()
        {
            InitializeComponent();
            LoadSettings();
            LoadTables();
        }
        private void LoadSettings()
        {
            txtServer.Text = ConfigurationManager.AppSettings["DbServer"];
            txtDatabase.Text = ConfigurationManager.AppSettings["DbName"];
            txtUser.Text = ConfigurationManager.AppSettings["DbUser"];
            txtPassword.Text = ConfigurationManager.AppSettings["DbPassword"];
        }
        private void LoadTables()
        {
            var dbConnection = new DatabaseConnection();
            using (var connection = dbConnection.GetConnection())
            {
                try
                {
                    connection.Open();
                    DataTable tables = connection.GetSchema("Tables");
                    NameTable.DataSource = tables.Rows.Cast<DataRow>()
                       .Select(row => row["TABLE_NAME"].ToString())
                       .ToList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при загрузке таблиц " + ex.Message);
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                        connection.Close();
                }
            }
        }
        private void CSVimportBTN_Click(object sender, EventArgs e)
        {
            var dbConnection = new DatabaseConnection();
            using (var connection = dbConnection.GetConnection())
            {
                if (NameTable.SelectedItem == null)
                {
                    MessageBox.Show("Пожалуйста выберите таблицу.");
                    return;
                }

                OpenFileDialog openFileD = new OpenFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv",
                    Title = "Выберите CSV файл"
                };

                if (openFileD.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var csvData = File.ReadAllLines(openFileD.FileName, Encoding.GetEncoding(1251));
                        var columns = csvData[0].Split(';');

                        connection.Open();

                        MySqlCommand setNamesCmd = new MySqlCommand("SET NAMES 'utf8';", connection);
                        setNamesCmd.ExecuteNonQuery();

                        string tableName = NameTable.SelectedItem.ToString();
                        if (IsMySqlReservedWord(tableName))
                        {
                            tableName = $"`{tableName}`";
                        }

                        // Получаем метаданные таблицы
                        MySqlCommand cmd3 = new MySqlCommand($"SELECT * FROM {tableName} LIMIT 0", connection);
                        MySqlDataAdapter ad = new MySqlDataAdapter(cmd3);
                        DataTable dt = new DataTable();
                        ad.Fill(dt);

                        if (columns.Length != dt.Columns.Count)
                        {
                            MessageBox.Show("Кол-во колонок в файле не совпадает с выбранной таблицей");
                            return;
                        }

                        foreach (var line in csvData.Skip(1))
                        {
                            var values = line.Split(';');
                            var formattedValues = new List<string>();

                            for (int i = 0; i < values.Length; i++)
                            {
                                if (string.IsNullOrEmpty(values[i]))
                                {
                                    formattedValues.Add("NULL");
                                }
                                else if (dt.Columns[i].ColumnName == "VAR")
                                {
                                    formattedValues.Add(values[i]);
                                }
                                else
                                {
                                    formattedValues.Add($"'{MySqlHelper.EscapeString(values[i])}'");
                                }
                            }

                            var query = $"INSERT INTO {tableName} VALUES ({string.Join(", ", formattedValues)})";
                            MySqlCommand insertCmd3 = new MySqlCommand(query, connection);
                            insertCmd3.ExecuteNonQuery();
                        }

                        MessageBox.Show("Данные импортированы.");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при импорте: " + ex.Message);
                    }
                    finally
                    {
                        if (connection.State == ConnectionState.Open)
                            connection.Close();
                    }
                }
            }
        }

        private bool IsMySqlReservedWord(string word)
        {
            var reservedWords = new List<string> { "order", "select", "insert", "update", "delete", "where", "group", "table" };
            return reservedWords.Contains(word.ToLower());
        }


        private void HealthBDbtn_Click(object sender, EventArgs e)
        {
            var dbConnection = new DatabaseConnection();
            using (var connection = dbConnection.GetConnection())
            {
                if (NameTable.SelectedItem == null)
                {
                    MessageBox.Show("Пожалуйста выберите файл.");
                    return;
                }
                OpenFileDialog openFileD = new OpenFileDialog
                {
                    Filter = "sql files (*.sql)|*.sql",
                    Title = "Выберите sql файл"
                };
                try
                {
                    if (openFileD.ShowDialog() == DialogResult.OK)
                    {
                        var scr = File.ReadAllText(openFileD.FileName);
                        MySqlCommand cmd2 = new MySqlCommand(scr, connection);
                        connection.Open();
                        cmd2.ExecuteNonQuery();
                        LoadTables();
                        MessageBox.Show("База данных восстановлена успешно.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при чтении: " + ex.Message);
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                        connection.Close();
                }
            }
        }

        private void NameTable_SelectedIndexChanged(object sender, EventArgs e)
        {
            CSVimportBTN.Enabled = NameTable.SelectedItem != null;
        }

        private void TestConnection_Click_1(object sender, EventArgs e)
        {
            try
            {
                string testConnectionString =
                    $"server={txtServer.Text};database={txtDatabase.Text};" +
                    $"user={txtUser.Text};password={txtPassword.Text};";

                using (var testConn = new MySqlConnection(testConnectionString))
                {
                    testConn.Open();
                    MessageBox.Show("Поключение успешно!", "Проверка подключения", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка подключения:\n" + ex, "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSave_Click_1(object sender, EventArgs e)
        {
            try
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

                config.AppSettings.Settings["DbServer"].Value = txtServer.Text;
                config.AppSettings.Settings["DbName"].Value = txtDatabase.Text;
                config.AppSettings.Settings["DbUser"].Value = txtUser.Text;
                config.AppSettings.Settings["DbPassword"].Value = txtPassword.Text;

                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");

                MessageBox.Show("Настройки сохраненны!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения настроек:\n{ex.Message}", "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DbSettingsForm_FormClosing(object sender, FormClosingEventArgs e)
        {

        }
    }
}

