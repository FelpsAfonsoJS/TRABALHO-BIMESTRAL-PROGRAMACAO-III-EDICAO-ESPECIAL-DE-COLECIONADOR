using Microsoft.EntityFrameworkCore;
using umfg.programacaoiii.minimalapi._2026.Entidades;

namespace umfg.programacaoiii.minimalapi._2026.Contexto;

public sealed class MySqlContexto : DbContext
{
	public DbSet<Lembrete> Lembrete { get; set; }

	public MySqlContexto(DbContextOptions<MySqlContexto> options)
		: base(options)
	{
		if (Database.GetPendingMigrations().Any())
			Database.Migrate();
	}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<Lembrete>().HasKey(x => x.Id);
		modelBuilder.Entity<Lembrete>().Property(x => x.Descricao).HasMaxLength(500).IsRequired();
		modelBuilder.Entity<Lembrete>().Property(x => x.DataCadastro).IsRequired();
		modelBuilder.Entity<Lembrete>().Property(x => x.DataAtualizacao).IsRequired();
		modelBuilder.Entity<Lembrete>().Property(x => x.IsAtivo).IsRequired();
    }
}
