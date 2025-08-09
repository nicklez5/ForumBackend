namespace MyApi.Models;

public class ThreadLike
{
    public int Id { get; set; }
    public string ApplicationUserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public int ThreadId { get; set; }
    public Threads? Thread { get; set; }

}
public class ThreadVote
{
    public int Id { get; set; }
    public int ThreadId { get; set; }
    public Threads? Thread { get; set; }
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }
    public int Value { get; set; }
}
public class ThreadVoteDto
{
    public int ThreadId { get; set; }
    public int Vote { get; set; }
}