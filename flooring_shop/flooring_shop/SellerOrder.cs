﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace flooring_shop
{
    public partial class SellerOrder : Form
    {
        private DatabaseConnection dbConnection;
        private List<Product> selectedProducts = new List<Product>(); // Список выбранных товаров
        private DataTable originalDataTable; // Оригинальные данные из базы

        public SellerOrder()
        {
            InitializeComponent();
            dbConnection = new DatabaseConnection();
            InitializeSortComboBox(); // Инициализируем ComboBox для сортировки
            InitializeDataGridView(); // Настраиваем DataGridView
            LoadCategories(); // Загружаем категории
            LoadProducts(); // Загружаем товары при запуске формы
        }

        // Загрузка категорий в ComboBox
        private void LoadCategories()
        {
            try
            {
                if (dbConnection.OpenConnection())
                {
                    string query = "SELECT DISTINCT ProdCategoryName FROM ProdCategory";
                    MySqlDataAdapter adapter = new MySqlDataAdapter(query, dbConnection.GetConnection());
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    // Добавляем пункт "Все категории"
                    DataRow allCategoriesRow = dataTable.NewRow();
                    allCategoriesRow["ProdCategoryName"] = "Все категории";
                    dataTable.Rows.InsertAt(allCategoriesRow, 0);

                    CmbFilter.DataSource = dataTable;
                    CmbFilter.DisplayMember = "ProdCategoryName"; // Отображаемое значение
                    CmbFilter.SelectedIndex = 0; // Выбираем "Все категории" по умолчанию

                    dbConnection.CloseConnection();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке категорий: " + ex.Message);
            }
        }
        private void ApplyConditionalFormatting()
        {
            // Убедимся, что колонка "Остаток" существует
            if (!DGVProdOrderSeller.Columns.Contains("Остаток"))
                return;

            foreach (DataGridViewRow row in DGVProdOrderSeller.Rows)
            {
                if (row.Cells["Остаток"].Value != null)
                {
                    int quantity = Convert.ToInt32(row.Cells["Остаток"].Value);

                    // Форматирование в зависимости от количества
                    if (quantity <= 0)
                    {
                        // Нет в наличии
                        row.DefaultCellStyle.BackColor = Color.LightCoral;
                        row.DefaultCellStyle.ForeColor = Color.DarkRed;
                    }
                    else if (quantity <= 200)
                    {
                        // Мало товара
                        row.DefaultCellStyle.BackColor = Color.LightGoldenrodYellow;
                        row.DefaultCellStyle.ForeColor = Color.DarkGoldenrod;
                    }
                    else
                    {
                        // Достаточно товара
                        row.DefaultCellStyle.BackColor = Color.LightGreen;
                        row.DefaultCellStyle.ForeColor = Color.DarkGreen;
                    }
                }
            }
        }
        public void LoadProducts()
        {
            try
            {
                if (dbConnection.OpenConnection())
                {
                    string query = @"
                SELECT
                    ProductArticleNumber AS 'Артикул',
                    ProductName AS 'Название',
                    ProductUnit AS 'Единица',
                    ProductCost AS 'Цена',
                    ProductQuantityInStock AS 'Остаток',
                    ProductDescription AS 'Описание',
                    prodcategory.ProdCategoryName AS 'Категория'
                FROM Product INNER JOIN prodcategory ON ProductCategory = ProdCategoryID";

                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(query, dbConnection.GetConnection()))
                    {
                        originalDataTable = new DataTable();
                        adapter.Fill(originalDataTable);
                        DGVProdOrderSeller.DataSource = originalDataTable;

                        // Применяем условное форматирование после загрузки данных
                        ApplyConditionalFormatting();
                    }
                    dbConnection.CloseConnection();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке данных: " + ex.Message);
            }
        }

        // Инициализация ComboBox для сортировки
        private void InitializeSortComboBox()
        {
            CmbSort.Items.AddRange(new string[]
            {
                "Без сортировки",
                "Цена по возрастанию",
                "Цена по убыванию",
                "Название по алфавиту (А-Я)",
                "Название по алфавиту (Я-А)"
            });
            CmbSort.SelectedIndex = 0;
        }

        // Настройка DataGridView
        private void InitializeDataGridView()
        {
            DGVProdOrderSeller.ReadOnly = true;
            DGVProdOrderSeller.AllowUserToDeleteRows = false;
            DGVProdOrderSeller.AllowUserToAddRows = false;
            DGVProdOrderSeller.AllowUserToResizeRows = false;
            DGVProdOrderSeller.AllowUserToResizeColumns = false;
            DGVProdOrderSeller.AutoGenerateColumns = true;
        }

        // Обновление данных в DataGridView с учетом фильтрации, поиска и сортировки
        private void UpdateDataGridView()
        {
            if (originalDataTable == null) return;

            DataTable filteredDataTable = originalDataTable.Copy();

            // Применяем фильтр по категории
            string selectedCategory = CmbFilter.Text;
            if (selectedCategory != "Все категории")
            {
                // Фильтруем по названию категории
                filteredDataTable.DefaultView.RowFilter = $"[Категория] = '{selectedCategory}'";
            }
            else
            {
                // Если выбрано "Все категории", сбрасываем фильтр
                filteredDataTable.DefaultView.RowFilter = "";
            }

            // Применяем поиск
            string searchText = TxtSearch.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(searchText))
            {
                // Добавляем условие поиска
                string searchFilter = $"[Название] LIKE '%{searchText}%' OR [Артикул] LIKE '%{searchText}%'";
                if (string.IsNullOrEmpty(filteredDataTable.DefaultView.RowFilter))
                {
                    filteredDataTable.DefaultView.RowFilter = searchFilter;
                }
                else
                {
                    filteredDataTable.DefaultView.RowFilter += $" AND ({searchFilter})";
                }
            }

            // Применяем сортировку
            string sortBy = CmbSort.Text;
            switch (sortBy)
            {
                case "Цена по возрастанию":
                    filteredDataTable.DefaultView.Sort = "[Цена] ASC";
                    break;
                case "Цена по убыванию":
                    filteredDataTable.DefaultView.Sort = "[Цена] DESC";
                    break;
                case "Название по алфавиту (А-Я)":
                    filteredDataTable.DefaultView.Sort = "[Название] ASC";
                    break;
                case "Название по алфавиту (Я-А)":
                    filteredDataTable.DefaultView.Sort = "[Название] DESC";
                    break;
                default:
                    filteredDataTable.DefaultView.Sort = "";
                    break;
            }
            // Обновляем DataGridView
            DGVProdOrderSeller.DataSource = filteredDataTable.DefaultView.ToTable();
            ApplyConditionalFormatting();
        }
 
        // Обработчик изменения выбора в ComboBox для фильтрации
        private void CmbFilter_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            UpdateDataGridView();
        }

        // Обработчик изменения текста в TextBox для поиска
        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            UpdateDataGridView();
        }

        // Обработчик изменения выбора в ComboBox для сортировки
        private void CmbSort_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            UpdateDataGridView();
        }

        // Обработчик нажатия кнопки "Добавить в заказ"
        private void AddOrder_Click(object sender, EventArgs e)
        {
            if (DGVProdOrderSeller.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow row in DGVProdOrderSeller.SelectedRows)
                {
                    // Создаем объект Product и заполняем его данными из DataGridView
                    Product product = new Product
                    {
                        Article = row.Cells["Артикул"].Value.ToString(),
                        Name = row.Cells["Название"].Value.ToString(),
                        Price = Convert.ToDecimal(row.Cells["Цена"].Value),
                        QuantityInStock = Convert.ToInt32(row.Cells["Остаток"].Value), // Количество на складе
                        QuantityOrdered = 1 // По умолчанию 1 шт.
                    };

                    // Проверяем, есть ли товар уже в списке
                    Product existingProduct = selectedProducts.FirstOrDefault(p => p.Article == product.Article);
                    if (existingProduct != null)
                    {
                        // Увеличиваем количество, если товар уже есть в списке
                        existingProduct.QuantityOrdered += 1;
                    }
                    else
                    {
                        // Добавляем товар в список, если его там нет
                        selectedProducts.Add(product);
                    }
                }
                
                // Показываем сообщение о добавлении
                MessageBox.Show("Товары добавлены в заказ.", "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Выберите товары для добавления в заказ\n(с помощью выделения строки).", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Обработчик нажатия кнопки "Оформить заказ"
        private void OrderBtn_Click(object sender, EventArgs e)
        {
            if (selectedProducts.Count > 0)
            {
                // Открываем форму SellerOrderGo с выбранными товарами
                SellerOrderGo orderForm = new SellerOrderGo(selectedProducts, this);
                orderForm.ShowDialog();
            }
            else
            {
                MessageBox.Show("Добавьте товары в заказ\n(с помощью выделения строки).", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            HistorySeller orderseller = new HistorySeller();
            this.Hide();
            orderseller.Show();
        }

        // Обработчик закрытия формы
        private void SellerOrder_FormClosed(object sender, FormClosedEventArgs e)
        {
            Authorization authForm = new Authorization();
            authForm.Show();
        }

        private void SellerOrder_Load(object sender, EventArgs e)
        {
            LoadProducts();
        }
    }
}