using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace mr_poul
{
    public partial class Form2 : Form
    {
        private NpgsqlConnection conn;
        private NpgsqlDataAdapter adapter;
        private DataTable dt;
        private Dictionary<string, Dictionary<int, string>> lookupData = new Dictionary<string, Dictionary<int, string>>();
        private bool changed;

        public Form2()
        {
            InitializeComponent();
            string connection = "Host=localhost;Port=5432;Database=mr_popul;Username=postgres;Password=11111111;";
            conn = new NpgsqlConnection(connection);
            comboBox1.Items.AddRange(new object[] { "partner_products", "products", "partners", "material_types", "product_types", "materials", "users" });
            comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
            comboBox1.SelectedIndex = 0;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string table = comboBox1.Text;

            LoadLookupTables();

            string query = $"SELECT * FROM {table}";
            adapter = new NpgsqlDataAdapter(query, conn); 
            var builder = new NpgsqlCommandBuilder(adapter); 

            dt = new DataTable();
            adapter.Fill(dt);
            dataGridView1.Columns.Clear();
            dataGridView1.DataSource = dt;
            HidePrimaryKey(table);
            ReplaceForeignKeyColumns(table);
            dataGridView1.DefaultValuesNeeded -= dataGridView1_DefaultValuesNeeded;
            dataGridView1.DefaultValuesNeeded += dataGridView1_DefaultValuesNeeded;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                adapter.Update(dt);
                MessageBox.Show("Данные успешно сохранены");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count != 0)
            {
                foreach(DataGridViewRow row in dataGridView1.SelectedRows){
                    if (!row.IsNewRow)
                    {
                        dataGridView1.Rows.Remove(row);
                    }
                }
                MessageBox.Show("Строки удалены. Не забудьте сохранить внесённые изменения перед переходом в другую таблицу.");
            }            
        }

        private void LoadLookupTables()
        {
            lookupData["products"] = LoadLookup("products", "product_id", "product_name");
            lookupData["partners"] = LoadLookup("partners", "partner_id", "partner_name");
            lookupData["product_types"] = LoadLookup("product_types", "type_id", "product_type");
            lookupData["material_types"] = LoadLookup("material_types", "type_id", "material_type");
        }

        private Dictionary<int, string> LoadLookup(string table, string keyCol, string valCol)
        {
            var dict = new Dictionary<int, string>();
            using (var cmd = new NpgsqlCommand($"SELECT {keyCol}, {valCol} FROM {table}", conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        dict[reader.GetInt32(0)] = reader.GetString(1);
                    }
                }
                conn.Close();
            }
            return dict;
        }
        private void HidePrimaryKey(string table)
        {
            string pkColumn = null;

            switch (table)
            {
                case "partner_products":
                    pkColumn = "sale_id";
                    break;
                case "products":
                    pkColumn = "product_id";
                    break;
                case "partners":
                    pkColumn = "partner_id";
                    break;
                case "material_types":
                    pkColumn = "type_id";
                    break;
                case "product_types":
                    pkColumn = "type_id";
                    break;
                case "materials":
                    pkColumn = "material_id";
                    break;
            }

            
            if (pkColumn != null && dataGridView1.Columns.Contains(pkColumn))
            {
                dataGridView1.Columns[pkColumn].Visible = false;
            }
        }

        private void ReplaceForeignKeyColumns(string table)
        {
            switch (table)
            {
                case "partner_products":
                    ReplaceWithComboBox("product_id", "products");
                    ReplaceWithComboBox("partner_id", "partners");
                    break;
                case "products":
                    ReplaceWithComboBox("product_type_id", "product_types");
                    break;
                case "materials":
                    ReplaceWithComboBox("material_type_id", "material_types");
                    break;
            }
        }

        private void ReplaceWithComboBox(string columnName, string lookupTable)
        {
            if (!dt.Columns.Contains(columnName))
                return;

            if (dataGridView1.Columns.Contains(columnName))
                dataGridView1.Columns.Remove(columnName);

            var combo = new DataGridViewComboBoxColumn
            {
                Name = columnName,
                DataPropertyName = columnName,
                HeaderText = columnName.Replace("_id", ""),
                DataSource = new BindingSource(lookupData[lookupTable], null),
                DisplayMember = "Value", 
                ValueMember = "Key",
                FlatStyle = FlatStyle.Flat
            };

            int index = dataGridView1.Columns.Count > 0 ? dataGridView1.Columns[columnName]?.Index ?? 0 : 0;
            dataGridView1.Columns.Insert(index, combo);
        }

        private void dataGridView1_DefaultValuesNeeded(object sender, DataGridViewRowEventArgs e)
        {
            string table = comboBox1.Text;
            switch (table)
            {
                case "partner_products":
                    SetDefaultValue(e.Row, "product_id", "products");
                    SetDefaultValue(e.Row, "partner_id", "partners");
                    e.Row.Cells["quantity"].Value = 1;
                    e.Row.Cells["sale_date"].Value = DateTime.Now;
                    break;
                case "products":
                    SetDefaultValue(e.Row, "product_type_id", "product_types");
                    e.Row.Cells["min_partner_price"].Value = 0.0m;
                    break;
                case "materials":
                    SetDefaultValue(e.Row, "material_type_id", "material_types");
                    e.Row.Cells["purchase_price"].Value = 0.0m;
                    e.Row.Cells["stock_quantity"].Value = 0;
                    break;
            }
        }

        private void SetDefaultValue(DataGridViewRow row, string column, string lookupTable)
        {
            if (lookupData.ContainsKey(lookupTable) && lookupData[lookupTable].Any())
            {
                row.Cells[column].Value = lookupData[lookupTable].Keys.First();
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            Application.Exit();
        }
    }
}