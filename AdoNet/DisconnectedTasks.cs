using System.Data;
using DatabaseCourseExam.Data;
using Npgsql;

namespace DatabaseCourseExam.AdoNet;

public static class DisconnectedTasks
{
    public static async Task<DataTable> LoadStudentsAsync()
    {
        var dataSet = new DataSet();

        await using var connection = DatabaseConnection.Create();
        await connection.OpenAsync();

        var studentsTable = new DataTable("Students");
        const string studentsSql = """
            SELECT id, first_name, last_name, age, group_id
            FROM students
            ORDER BY id;
            """;

        await using var cmdStudents = new NpgsqlCommand(studentsSql, connection);
        using var adapterStudents = new NpgsqlDataAdapter(cmdStudents);
        adapterStudents.Fill(studentsTable);
        studentsTable.PrimaryKey = [studentsTable.Columns["id"]!];
        dataSet.Tables.Add(studentsTable);

        // TODO (задание 6): Добавить таблицу записей на курсы и вывести количество записей для каждого студента.
        var coursesTable = new DataTable("StudentCourses");
        const string coursesSql = """
            SELECT student_id, course_id
            FROM student_courses;
            """;

        await using var cmdCourses = new NpgsqlCommand(coursesSql, connection);
        using var adapterCourses = new NpgsqlDataAdapter(cmdCourses);
        adapterCourses.Fill(coursesTable);
        dataSet.Tables.Add(coursesTable);

        
        var relation = new DataRelation(
            "StudentCoursesRelation",
            studentsTable.Columns["id"]!,
            coursesTable.Columns["student_id"]!);
        dataSet.Relations.Add(relation);

       
        Console.WriteLine("--- Количество записей на курсы для каждого студента ---");
        foreach (DataRow studentRow in studentsTable.Rows)
        {
            DataRow[] childRows = studentRow.GetChildRows(relation);
            Console.WriteLine(
                $"Студент {studentRow["first_name"]} {studentRow["last_name"]} (ID: {studentRow["id"]}) " +
                $"записан на курсов: {childRows.Length}");
        }
        Console.WriteLine("--------------------------------------------------------");

        // TODO (задание 5): Реализовать добавление новой строки в DataTable с проверкой возраста и правильным RowState Added.
        DataRow newRow = studentsTable.NewRow();

        int maxId = 0;
        foreach (DataRow r in studentsTable.Rows)
        {
            int currentId = Convert.ToInt32(r["id"]);
            if (currentId > maxId) maxId = currentId;
        }

        newRow["id"] = maxId + 1;
        newRow["first_name"] = "Новый";
        newRow["last_name"] = "Студент";
        newRow["group_id"] = DBNull.Value;

        int newAge = 25;
        if (newAge >= 14 && newAge <= 100)
        {
            newRow["age"] = newAge;
            studentsTable.Rows.Add(newRow);
            Console.WriteLine($"Задание 5: Добавлена новая строка. RowState: {newRow.RowState}");
        }
        else
        {
            Console.WriteLine("Задание 5: Возраст не проходит проверку (должен быть от 14 до 100). Строка не добавлена.");
        }

        return studentsTable;
    }

    public static void PrintTable(DataTable table)
    {
        foreach (DataRow row in table.Rows)
        {
            Console.WriteLine(
                $"{row["id"]}: {row["first_name"]} " +
                $"{row["last_name"]}, возраст {row["age"]}");
        }
    }
    // TODO (задание 1): Найти строку по первичному ключу, обработать отсутствие результата и не менять строку, если значение не изменилось.
    public static void ChangeAgeInMemory(
        DataTable table,
        int studentId,
        int newAge)
    {
        DataRow? row = table.Rows.Find(studentId);

        if (row is null)
        {
            Console.WriteLine($"Студент с ID {studentId} не найден в DataTable.");
            return;
        }

        int currentAge = Convert.ToInt32(row["age"]);
        if (currentAge == newAge)
        {
            Console.WriteLine("Возраст не изменился. Строка не модифицирована (RowState остался Unchanged).");
            return;
        }

        row["age"] = newAge;

        Console.WriteLine(
            $"Возраст студента {studentId} успешно изменён на {newAge}. Состояние строки: {row.RowState}");
    }

    public static int SaveChanges(DataTable table)
    {
        using var connection = DatabaseConnection.Create();
        connection.Open();

        const string sql = """
        UPDATE students
        SET age = @age
        WHERE id = @id;
        """;

        using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.Add("@age", NpgsqlTypes.NpgsqlDbType.Integer, 4, "age");
        command.Parameters.Add("@id", NpgsqlTypes.NpgsqlDbType.Integer, 4, "id");

        using var adapter = new NpgsqlDataAdapter();
        adapter.UpdateCommand = command;

        int affectedRows = adapter.Update(table);
        Console.WriteLine($"Сохранено изменений: {affectedRows}");

        return affectedRows;
    }
}