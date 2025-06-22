# Task Manager

This is a complete task management application built with WinForms (C#) and PostgreSQL. The application allows you to manage workers and tasks and assign tasks to workers based on required skills.

## Key Features

- **Worker Management**: Add, edit, and delete workers with their skills
- **Task Management**: Create, edit, and delete tasks with required skills
- **Skill Tracking**: Automatically tracks and manages skills for both workers and tasks
- **Task Assignment**: Assign tasks to workers who have all required skills
- **Relationship Management**: Automatically handles relationships between workers, tasks, and skills

## Requirements

- .NET Framework 4.8
- PostgreSQL 12+
- Npgsql ADO.NET provider

## Installation and Setup

### 1. Database Setup

1. Install PostgreSQL (if not already installed)
2. Create a new database named `task_manager`
3. Execute the following SQL script to create tables:

```sql
CREATE TABLE workers (
    id SERIAL PRIMARY KEY,
    first_name TEXT NOT NULL,
    last_name TEXT NOT NULL,
    middle_name TEXT,
    email TEXT NOT NULL,
    is_busy BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE TABLE tasks (
    id SERIAL PRIMARY KEY,
    title TEXT NOT NULL,
    description TEXT,
    is_busy BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE TABLE skills (
    id SERIAL PRIMARY KEY,
    name TEXT NOT NULL UNIQUE
);

CREATE TABLE worker_skills (
    id SERIAL PRIMARY KEY,
    worker_id INTEGER REFERENCES workers(id) ON DELETE CASCADE,
    skill_id INTEGER REFERENCES skills(id) ON DELETE CASCADE
);

CREATE TABLE task_skills (
    id SERIAL PRIMARY KEY,
    task_id INTEGER REFERENCES tasks(id) ON DELETE CASCADE,
    skill_id INTEGER REFERENCES skills(id) ON DELETE CASCADE
);

CREATE TABLE assignments (
    id SERIAL PRIMARY KEY,
    worker_id INTEGER REFERENCES workers(id) ON DELETE CASCADE,
    task_id INTEGER REFERENCES tasks(id) ON DELETE CASCADE
);
```

### 2. Application Configuration

1. Clone the repository:
   ```bash
   git clone https://github.com/your-username/task-manager.git
   ```

2. Open the solution in Visual Studio (2019 or newer)

3. Update the connection string in `App.config`:
   ```xml
   <connectionStrings>
       <add name="DefaultConnection" 
            connectionString="Server=localhost;Port=5432;Database=task_manager;User Id=your_username;Password=your_password;" />
   </connectionStrings>
   ```

4. Install required NuGet packages:
   - Npgsql

## Running the Application

1. Build the solution in Visual Studio
2. Run the project
3. The main form will appear with two buttons:
   - **Manage Workers**: Opens worker management form
   - **Manage Tasks**: Opens task management form

## Application Structure

### Forms

- **MainForm.cs**: The entry point with navigation buttons
- **WorkersForm.cs**: Manages workers (add, edit, delete)
- **TasksForm.cs**: Manages tasks (add, edit, delete)
- **AddWorkerForm.cs**: Form for adding new workers
- **AddTaskForm.cs**: Form for adding new tasks
- **EditWorkerForm.cs**: Form for editing worker details
- **EditTaskForm.cs**: Form for editing task details
- **AssignTaskForm.cs**: Form for assigning tasks to workers

### Database Helper

- **DBHelper.cs**: Provides database connection management

## Key Functionality

### Worker Management
- Add workers with their skills (entered as space-separated values)
- Edit worker details and skills
- Delete workers (automatically frees assigned tasks)

### Task Management
- Create tasks with required skills (entered as space-separated values)
- Edit task details and required skills
- Delete tasks (automatically frees assigned workers)

### Task Assignment
- Assign tasks to workers who have all required skills
- Automatic status update (worker and task marked as busy)
- Prevent assignment to busy workers or already assigned tasks

### Data Management
- Automatic relationship management
- Cascading deletes
- Status synchronization between related entities

## Troubleshooting

If you encounter connection issues:
1. Verify PostgreSQL service is running
2. Check connection string parameters in App.config
3. Ensure firewall allows connections to PostgreSQL (default port 5432)

For application errors:
- Check error messages for specific details
- Verify database tables match the provided schema
