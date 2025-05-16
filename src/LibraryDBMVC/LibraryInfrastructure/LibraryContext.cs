using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using LibraryDomain.Model;
using LibraryInfrastructure.Models;

namespace LibraryInfrastructure;

public partial class LibraryContext : IdentityDbContext<ApplicationUser>
{
    public LibraryContext()
    {
    }

    public LibraryContext(DbContextOptions<LibraryContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Author> Authors { get; set; }

    public virtual DbSet<AuthorsBook> AuthorsBooks { get; set; }

    public virtual DbSet<Book> Books { get; set; }

    public virtual DbSet<Client> Clients { get; set; }

    public virtual DbSet<ClientsBook> ClientsBooks { get; set; }

    public virtual DbSet<Genre> Genres { get; set; }

    public virtual DbSet<GenresBook> GenresBooks { get; set; }

    public virtual DbSet<Publisher> Publishers { get; set; }

    public virtual DbSet<Worker> Workers { get; set; }

    public DbSet<BookReservation> BookReservations { get; set; }

    public DbSet<WorkerSearchResult> WorkerSearchResults { get; set; } = null!;

    public DbSet<BookStatusSearchResult> BookStatusSearchResult { get; set; } = null!;

    public DbSet<BookExcludePublisherResult> BookExcludePublisherResult { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Server=Asus\\SQLEXPRESS; Database=Library; Trusted_Connection=True; TrustServerCertificate=True; ");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        OnModelCreatingPartial(modelBuilder);

        modelBuilder.Entity<ClientsBook>()
        .HasOne(cb => cb.Book)
        .WithMany(b => b.ClientsBooks)
        .HasForeignKey(cb => cb.BookId);

        modelBuilder.Entity<ClientsBook>()
            .HasOne(cb => cb.Client)
            .WithMany(c => c.ClientsBooks)
            .HasForeignKey(cb => cb.ClientId)
            .HasPrincipalKey(c => c.Id);

        modelBuilder.Entity<Author>(entity =>
        {
            entity.Property(e => e.Country).HasMaxLength(50);
            entity.Property(e => e.FullName)
                .HasMaxLength(50)
                .HasColumnName("Full name");
        });

        modelBuilder.Entity<AuthorsBook>(entity =>
        {
            entity.Property(e => e.AuthorId).HasColumnName("Author id");
            entity.Property(e => e.BookId).HasColumnName("Book id");

            entity.HasOne(d => d.Author).WithMany(p => p.AuthorsBooks)
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AuthorsBooks_Authors");

            entity.HasOne(d => d.Book).WithMany(p => p.AuthorsBooks)
                .HasForeignKey(d => d.BookId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AuthorsBooks_Books");
        });

        modelBuilder.Entity<Book>(entity =>
        {
            entity.Property(e => e.AddedBy).HasColumnName("Added by");
            entity.Property(e => e.PublisherId).HasColumnName("Publisher id");
            entity.Property(e => e.Title).HasMaxLength(50);

            entity.HasOne(d => d.Worker).WithMany(p => p.Books)
                .HasForeignKey(d => d.AddedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Books_Workers");

            entity.HasOne(d => d.Publisher).WithMany(p => p.Books)
                .HasForeignKey(d => d.PublisherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Books_Publishers");
        });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.Property(e => e.FullName)
                .HasMaxLength(50)
                .HasColumnName("Full name");
        });

        modelBuilder.Entity<ClientsBook>(entity =>
        {
            entity.Property(e => e.BookId).HasColumnName("Book id");
            entity.Property(e => e.BorrowingDate).HasColumnName("Borrowing date");
            entity.Property(e => e.ClientId).HasColumnName("Client id");
            entity.Property(e => e.ReturnDate).HasColumnName("Return date");

            entity.HasOne(d => d.Book).WithMany(p => p.ClientsBooks)
                .HasForeignKey(d => d.BookId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ClientsBooks_Books");

            entity.HasOne(d => d.Client).WithMany(p => p.ClientsBooks)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ClientsBooks_Clients");
        });

        modelBuilder.Entity<Genre>(entity =>
        {
            entity.Property(e => e.GenreName)
                .HasMaxLength(50)
                .HasColumnName("Genre name");
        });

        modelBuilder.Entity<GenresBook>(entity =>
        {
            entity.Property(e => e.BookId).HasColumnName("Book id");
            entity.Property(e => e.GenreId).HasColumnName("Genre id");

            entity.HasOne(d => d.Book).WithMany(p => p.GenresBooks)
                .HasForeignKey(d => d.BookId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GenresBooks_Books");

            entity.HasOne(d => d.Genre).WithMany(p => p.GenresBooks)
                .HasForeignKey(d => d.GenreId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GenresBooks_Genres");
        });

        modelBuilder.Entity<Publisher>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<Worker>(entity =>
        {
            entity.HasKey(w => w.Id);

            entity.Property(w => w.FullName)
                .HasMaxLength(50)
                .HasColumnName("Full name");

            entity.Property(w => w.ApplicationUserId)
                .IsRequired();

            entity.HasOne(w => w.ApplicationUser)
                .WithOne(u => u.Worker)
                .HasForeignKey<Worker>(w => w.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BookReservation>()
            .HasOne(br => br.User)
            .WithMany()
            .HasForeignKey(br => br.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BookSearchResult>().HasNoKey();

        modelBuilder.Entity<WorkerSearchResult>().HasNoKey();
        
        modelBuilder.Entity<BookStatusSearchResult>().HasNoKey();

        modelBuilder.Entity<BookExcludePublisherResult>().HasNoKey();

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
