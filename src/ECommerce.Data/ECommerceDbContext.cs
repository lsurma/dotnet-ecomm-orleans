using Microsoft.EntityFrameworkCore;

namespace ECommerce.Data;

/// <summary>
/// DbContext dla aplikacji e-commerce
/// 
/// W Orleans, DbContext jest używany głównie przez:
/// 1. Persystencję stanu grainów (Orleans.Persistence.AdoNet)
/// 2. Clustering i membership (Orleans.Clustering.AdoNet)
/// 
/// Orleans automatycznie zarządza zapisem/odczytem stanu grainów
/// - Nie musimy ręcznie wywoływać SaveChanges() dla stanu grainów
/// - Orleans robi to za nas gdy wywołujemy WriteStateAsync()
/// 
/// DbContext for e-commerce application
/// 
/// In Orleans, DbContext is mainly used by:
/// 1. Grain state persistence (Orleans.Persistence.AdoNet)
/// 2. Clustering and membership (Orleans.Clustering.AdoNet)
/// 
/// Orleans automatically manages grain state save/load
/// - We don't need to manually call SaveChanges() for grain state
/// - Orleans does it for us when we call WriteStateAsync()
/// </summary>
public class ECommerceDbContext : DbContext
{
    public ECommerceDbContext(DbContextOptions<ECommerceDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Orleans będzie używał własnych tabel do persystencji
        // Tabele są tworzone przez skrypty SQL Orleans
        // 
        // Orleans will use its own tables for persistence
        // Tables are created by Orleans SQL scripts
        //
        // Typowe tabele Orleans:
        // - OrleansStorage - przechowuje stan grainów
        // - OrleansMembershipTable - informacje o klastrze
        // - OrleansRemindersTable - przypomnienia (timers)
    }
}
