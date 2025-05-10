using Npgsql;
using System;
using System.Windows.Forms;

namespace mr_poul
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string name = textBox1.Text;
            string password = textBox2.Text;

            using (var conn = new NpgsqlConnection("Host=localhost;Port=5432;Database=mr_popul;Username=postgres;Password=11111111;"))
            {
                conn.Open();

                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "SELECT COUNT(*) FROM users WHERE login = @login AND password = @password";
                    cmd.Parameters.AddWithValue("login", name);
                    cmd.Parameters.AddWithValue("password", password);

                    int count = 0;
                    object result = cmd.ExecuteScalar();

                    if (result != null && int.TryParse(result.ToString(), out count))
                    {
                        if (count > 0)
                        {
                            MessageBox.Show("Вход успешен!");
                            Form form2 = new Form2();
                            this.Hide();    
                            form2.Show();     
                        }
                        else
                        {
                            MessageBox.Show("Неверное имя пользователя или пароль.");
                        }
                    }
                    conn.Close();
                }
            }
        }
    }
}