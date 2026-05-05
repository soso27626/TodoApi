using Microsoft.EntityFrameworkCore;

class Todo
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public bool IsComplete { get; set; }
    public DateTime? Deadline { get; set; }
    public string? Recurrence { get; set; }
}


class TodoDb : DbContext
{
    public TodoDb(DbContextOptions<TodoDb> options) : base(options) { }
    public DbSet<Todo> Todos => Set<Todo>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();

}

class JournalEntry
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Content { get; set; } = "";
}
