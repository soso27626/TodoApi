using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddDbContext<TodoDb>(opt => opt.UseSqlite("Data Source=todos.db"));
builder.Services.AddCors();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

app.MapGet("/todos", async (TodoDb db) =>
{
    var todos = await db.Todos.ToListAsync();
    var today = DateTime.Now.Date;
    
    foreach (var todo in todos)
    {
        if (todo.Recurrence != null && todo.Deadline.HasValue && todo.Deadline.Value.Date < today)
        {
            // 不管完没完成，只要过期了就推到下一个周期
            while (todo.Deadline.Value.Date < today)
            {
                todo.Deadline = todo.Recurrence switch
                {
                    "daily" => todo.Deadline.Value.AddDays(1),
                    "weekly" => todo.Deadline.Value.AddDays(7),
                    "biweekly" => todo.Deadline.Value.AddDays(14),
                    "yearly" => todo.Deadline.Value.AddYears(1),
                    _ => todo.Deadline
                };
            }
            todo.IsComplete = false;
        }
    }
    
    await db.SaveChangesAsync();
    return todos;
});


app.MapGet("/todos/{id}", async (int id, TodoDb db) =>
{
    var todo = await db.Todos.FindAsync(id);
    return todo is null ? Results.NotFound() : Results.Ok(todo);
});

app.MapPost("/todos", async (Todo todo, TodoDb db) =>
{
    db.Todos.Add(todo);
    await db.SaveChangesAsync();
    return Results.Created($"/todos/{todo.Id}", todo);
});

app.MapPut("/todos/{id}", async (int id, Todo input, TodoDb db) =>
{
    var todo = await db.Todos.FindAsync(id);
    if (todo is null) return Results.NotFound();
    todo.Title = input.Title;
    todo.IsComplete = input.IsComplete;
    await db.SaveChangesAsync();
    return Results.Ok(todo);
});

app.MapDelete("/todos/{id}", async (int id, TodoDb db) =>
{
    var todo = await db.Todos.FindAsync(id);
    if (todo is null) return Results.NotFound();
    db.Todos.Remove(todo);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapGet("/journal", async (TodoDb db) => await db.JournalEntries.ToListAsync());

app.MapPost("/journal", async (JournalEntry entry, TodoDb db) =>
{
    entry.Date = DateTime.Now.Date;
    db.JournalEntries.Add(entry);
    await db.SaveChangesAsync();
    return Results.Created($"/journal/{entry.Id}", entry);
});

app.MapGet("/journal/{date}", async (string date, TodoDb db) =>
{
    var d = DateTime.Parse(date).Date;
    var entry = await db.JournalEntries.FirstOrDefaultAsync(e => e.Date == d);
    return entry is null ? Results.NotFound() : Results.Ok(entry);
});

app.MapPut("/journal/{id}", async (int id, JournalEntry input, TodoDb db) =>
{
    var entry = await db.JournalEntries.FindAsync(id);
    if (entry is null) return Results.NotFound();
    entry.Content = input.Content;
    await db.SaveChangesAsync();
    return Results.Ok(entry);
});


app.Run();
