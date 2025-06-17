namespace Task_Manager
{
    partial class Form1
    {
        /// <summary>
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.btnAssignTasks = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.AccessibleDescription = "btnManageWorkers";
            this.button1.Location = new System.Drawing.Point(50, 30);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(200, 30);
            this.button1.TabIndex = 0;
            this.button1.Text = "Управление исполнителями";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.btnManageWorkers_Click);
            // 
            // button2
            // 
            this.button2.AccessibleDescription = "btnManageTasks";
            this.button2.Location = new System.Drawing.Point(50, 80);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(200, 30);
            this.button2.TabIndex = 1;
            this.button2.Text = "Управление задачами";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.btnManageTasks_Click);
            // 
            // btnAssignTasks
            // 
            this.btnAssignTasks.AccessibleDescription = "btnAssignTasks";
            this.btnAssignTasks.Location = new System.Drawing.Point(50, 130);
            this.btnAssignTasks.Name = "btnAssignTasks";
            this.btnAssignTasks.Size = new System.Drawing.Size(200, 30);
            this.btnAssignTasks.TabIndex = 2;
            this.btnAssignTasks.Text = "Назначить задачи";
            this.btnAssignTasks.UseVisualStyleBackColor = true;
            this.btnAssignTasks.Click += new System.EventHandler(this.btnAssignTasks_Click);
            // 
            // Form1
            // 
            this.ClientSize = new System.Drawing.Size(300, 190);
            this.Controls.Add(this.btnAssignTasks);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "Form1";
            this.Text = "Основное меню";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button btnAssignTasks;
    }
}

