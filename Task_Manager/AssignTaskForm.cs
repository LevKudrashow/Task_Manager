using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Npgsql;

namespace Task_Manager
{
    public partial class AssignTaskForm : Form
    {
        System.Windows.Forms.DataGridViewTextBoxColumn full_name = new System.Windows.Forms.DataGridViewTextBoxColumn();
        System.Windows.Forms.DataGridViewTextBoxColumn email = new System.Windows.Forms.DataGridViewTextBoxColumn();
        private int? _selectedTaskId = null;
        private bool? _workers = false;

        public AssignTaskForm()
        {
            InitializeComponent();
            LoadTasks();
        }

        private void LoadTasks()
        {
            using (var conn = DBHelper.GetConnection())
            {
                conn.Open();
                string query = @"
                SELECT 
                    t.id, 
                    t.title,
                    t.description,
                    t.is_busy
                FROM tasks t";

                var cmd = new NpgsqlCommand(query, conn);
                var adapter = new NpgsqlDataAdapter(cmd);
                var dt = new DataTable();
                adapter.Fill(dt);
                dataGridView.DataSource = dt;
            }
        }

        private void DataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != dataGridView.Columns["btnSelect"].Index)
                return;
            if (_workers == false)
            {
                int taskId = Convert.ToInt32(dataGridView.Rows[e.RowIndex].Cells["id"].Value);
                string isBusy = dataGridView.Rows[e.RowIndex].Cells["is_busy"].Value.ToString();

                if (isBusy == "True")
                {
                    MessageBox.Show("Задача уже занята!");
                    return;
                }
                _workers = true;
                _selectedTaskId = taskId;
                LoadWorkersForTask(taskId);
            }
            else
            {
                int workerId = Convert.ToInt32(dataGridView.Rows[e.RowIndex].Cells["id"].Value);
                string isBusy = dataGridView.Rows[e.RowIndex].Cells["is_busy"].Value.ToString();

                if (isBusy == "True")
                {
                    MessageBox.Show("Исполнитель уже занят!");
                    return;
                }
                AssignTask(_selectedTaskId.Value, workerId);
                this.Close();
            }
        }

        private void LoadWorkersForTask(int taskId)
        {

            dataGridView.DataSource = null;

            dataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.id,
            this.full_name,
            this.email,
            this.is_busy});
            // 
            // btnSelect
            // 
            this.btnSelect.HeaderText = "Выбрать";
            this.btnSelect.Name = "btnSelect";
            this.btnSelect.Text = "Выбрать";
            this.btnSelect.UseColumnTextForButtonValue = true;
            // 
            // full_name
            // 
            this.full_name.DataPropertyName = "full_name";
            this.full_name.FillWeight = 200F;
            this.full_name.HeaderText = "ФИО";
            this.full_name.Name = "full_name";
            // 
            // email
            // 
            this.email.DataPropertyName = "email";
            this.email.HeaderText = "Email";
            this.email.Name = "email";
            this.email.Width = 150;

            // Получаем навыки, требуемые для задачи
            List<int> requiredSkills = GetRequiredSkillsForTask(taskId);

            // Если нет требований, показываем всех свободных работников
            if (requiredSkills.Count == 0)
            {
                LoadAllFreeWorkers();
                return;
            }

            // Загрузка работников с нужными навыками
            using (var conn = DBHelper.GetConnection())
            {
                conn.Open();
                string query = @"
                SELECT 
                    w.id, 
                    w.last_name || ' ' || w.first_name || ' ' || w.middle_name AS full_name,
                    w.email,
                    w.is_busy
                FROM workers w
                WHERE w.is_busy = false AND w.id IN (
                    SELECT ws.worker_id
                    FROM worker_skills ws
                    WHERE ws.skill_id = ANY(@requiredSkills)
                    GROUP BY ws.worker_id
                    HAVING COUNT(DISTINCT ws.skill_id) = @skillsCount
                )";

                var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@requiredSkills", requiredSkills.ToArray());
                cmd.Parameters.AddWithValue("@skillsCount", requiredSkills.Count);

                var adapter = new NpgsqlDataAdapter(cmd);
                var dt = new DataTable();
                adapter.Fill(dt);
                dataGridView.DataSource = dt;
            }
        }

        private void LoadAllFreeWorkers()
        {
            using (var conn = DBHelper.GetConnection())
            {
                conn.Open();
                string query = @"
                SELECT 
                    id, 
                    last_name || ' ' || first_name || ' ' || middle_name AS full_name,
                    email,
                    is_busy
                FROM workers
                WHERE is_busy = false";

                var cmd = new NpgsqlCommand(query, conn);
                var adapter = new NpgsqlDataAdapter(cmd);
                var dt = new DataTable();
                adapter.Fill(dt);
                dataGridView.DataSource = dt;
            }
        }

        private List<int> GetRequiredSkillsForTask(int taskId)
        {
            List<int> skills = new List<int>();

            using (var conn = DBHelper.GetConnection())
            {
                conn.Open();
                var cmd = new NpgsqlCommand(
                    "SELECT skill_id FROM task_skills WHERE task_id = @taskId",
                    conn);
                cmd.Parameters.AddWithValue("@taskId", taskId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        skills.Add(reader.GetInt32(0));
                    }
                }
            }
            return skills;
        }

        private void AssignTask(int taskId, int workerId)
        {
            using (var conn = DBHelper.GetConnection())
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Добавление назначения
                        var cmd = new NpgsqlCommand(@"
                        INSERT INTO assignments (worker_id, task_id) 
                        VALUES (@wid, @tid)", conn);
                        cmd.Parameters.AddWithValue("@wid", workerId);
                        cmd.Parameters.AddWithValue("@tid", taskId);
                        cmd.ExecuteNonQuery();

                        // Обновление статусов
                        cmd = new NpgsqlCommand("UPDATE workers SET is_busy = true WHERE id = @id", conn);
                        cmd.Parameters.AddWithValue("@id", workerId);
                        cmd.ExecuteNonQuery();

                        cmd = new NpgsqlCommand("UPDATE tasks SET is_busy = true WHERE id = @id", conn);
                        cmd.Parameters.AddWithValue("@id", taskId);
                        cmd.ExecuteNonQuery();

                        transaction.Commit();
                        MessageBox.Show("Задача успешно назначена!");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show($"Ошибка: {ex.Message}");
                    }
                }
            }
        }
    }
}
