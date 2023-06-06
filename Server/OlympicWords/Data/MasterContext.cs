using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Collections;
using System.Linq.Expressions;
using System.Text.Json;
using OlympicWords.Data;

namespace OlympicWords.Services
{
    public class MasterContext : DbContext
    {
        public MasterContext(DbContextOptions<MasterContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserPicture> UserPictures { get; set; }
        public DbSet<ProviderLink> ExternalIds { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseLazyLoadingProxies();
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .Property(b => b.OwnedItemPlayers)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                    v => JsonSerializer.Deserialize<HashSet<string>>(v, JsonSerializerOptions.Default));

            modelBuilder.Entity<User>()
                .Property(b => b.SelectedItemPlayer)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, JsonSerializerOptions.Default));

            SetMaxLength(modelBuilder);

            modelBuilder.Entity<ProviderLink>()
                .HasOne(u => u.User)
                .WithMany(u => u.Providers)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserPicture>()
                .HasOne(u => u.User)
                .WithOne(u => u.Picture)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<UserPicture>()
                .HasKey(p => p.UserId);

            //user relations
            modelBuilder.Entity<User>()
                .HasMany(u => u.Followers)
                .WithMany(u => u.Followings)
                .UsingEntity<UserRelation>(
                    userRelation => userRelation
                        .HasOne(r => r.Follower)
                        .WithMany(u => u.FollowingRelations)
                        .HasForeignKey(r => r.FollowerId),
                    userRelation => userRelation
                        .HasOne(r => r.Following)
                        .WithMany(u => u.FollowerRelations)
                        .HasForeignKey(r => r.FollowingId),
                    userRelation =>
                        userRelation.HasKey(r => new { r.FollowerId, r.FollowingId }));

            IntListConversion(modelBuilder);

            Seeder.SeedData(modelBuilder);

            #region owned types tries
            //this is bad practically and performance wise
            // modelBuilder.Entity<User>()
            //     .OwnsOne(u => u.ItemPlayers, navBuilder =>
            //     {
            //         navBuilder.ToJson();
            //         navBuilder.OwnsMany(i => i.Owned);
            //         navBuilder.OwnsMany(i => i.Selected);
            //     });

            //this is not supported!
            // modelBuilder.Entity<User>().OwnsOne(u => u.OwnedPlayers, navBuilder => navBuilder.ToJson());
            // modelBuilder.Entity<User>().OwnsOne(u => u.SelectedPlayers, navBuilder => navBuilder.ToJson());

            // modelBuilder.Entity<UserItemPlayers>().HasData(
            //     new
            //     {
            //         OwnerId = "999",
            //         Owned = new HashSet<string> { "criminal" },
            //         Selected = new Dictionary<string, string> { { "GraphCityJump", "criminal" } },
            //     },
            //     new
            //     {
            //         OwnerId = "9999",
            //         Owned = new HashSet<string> { "criminal" },
            //         Selected = new Dictionary<string, string> { { "GraphCityJump", "criminal" } },
            //     },
            //     new
            //     {
            //         OwnerId = "9999",
            //         Owned = new HashSet<string> { "criminal" },
            //         Selected = new Dictionary<string, string> { { "GraphCityJump", "criminal" } },
            //     }
            // );


            // modelBuilder.Entity<User>().OwnsOne(u => u.SelectedPlayers,
            // ownedNavigationBuilder => { ownedNavigationBuilder.ToJson(); });

            // .HasData(
            //     new
            //     {
            //         Id = "999",
            //         SelectedPlayers = new Dictionary<string, string>
            //             { { "GraphCityJump", OfflineRepo.ItemPlayers[0].Id } }
            //     },
            // new
            // {
            //     Id = "9999",
            //     SelectedPlayers = new Dictionary<string, string>
            //         { { "GraphCityJump", OfflineRepo.ItemPlayers[0].Id } }
            // },
            // new
            // {
            //     Id = "99999",
            //     SelectedPlayers = new Dictionary<string, string>
            //         { { "GraphCityJump", OfflineRepo.ItemPlayers[0].Id } }
            // }
            // );

            // modelBuilder.Entity<User>(u =>
            // {
            //     u.HasData(Seeder.bot999);
            //
            //     u.OwnsOne(uu => uu.OwnedPlayers, navBuilder => { navBuilder.ToJson(); })
            //         .HasData(
            //             new { Id = "999", OwnedPlayers = new List<string> { OfflineRepo.ItemPlayers[0].Id } },
            //             new { Id = "9999", OwnedPlayers = new List<string> { OfflineRepo.ItemPlayers[0].Id } },
            //             new { Id = "99999", OwnedPlayers = new List<string> { OfflineRepo.ItemPlayers[0].Id } }
            //         );
            // });
            #endregion

            #region learning notes
            // modelBuilder.Entity<DisplayUser>(); this meas that I included this type in the database creation
            //despite it's not mentioned in a DbSet or explored by nav prop

            //you can ignore mentioned in exploring nav prop or even in a dbset by annotation or the fluent api
            //there's something called "exclude from migration" which is not the same as ignore, it's a bit advanced now

            //+++++++++++you need to learn
            //search for the schema in sql
            //also there's something called view in databases
            //nullable ref types, ms said it's recommended!
            //concurrency in ef core
            //optional: see indexer

            //configure prop by: moduleBuilder.Entity.Property

            //the key concept is the identifier for the row
            //the convention for pk is "Id" or "<TypeName>Id"

            //when the used pk type is not supported by the db, ef will create a temp one during the creation and tracking
            //then it will be replaced(in your code) with the real one after SaveChanges is called.

            //alternate key is a key that's not primary, has advanced usage

            //shadow prop is a prop not defined in the script model but defined in the database
            //it's the opposite of the ignored prop

            //I will use the value converter to create the comma separated id I want!
            #endregion
        }

        /// <summary>
        /// when you add a new environment, and you want to update the whole database to set the default selected players
        /// </summary>
        private void FillInMissingPlayers()
        {
            var total = Users.Count();
            const int size = 100;
            for (var pageNo = 0; pageNo < total / (float)size; pageNo++)
            {
                var chunk = Users.Skip(pageNo * size).Take(size).ToList();
                foreach (var user in chunk)
                {
                    foreach (var (key, value) in OfflineRepo.DefaultSelectedItemPlayers)
                    {
                        if (user.SelectedItemPlayer.ContainsKey(key)) continue;

                        user.SelectedItemPlayer.Add(key, value);
                        user.OwnedItemPlayers.Add(value);
                    }
                }

                SaveChanges();
            }
        }

        private void SetMaxLength(ModelBuilder modelBuilder)
        {
            var stringProps = modelBuilder.Model.GetEntityTypes().SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(string));

            foreach (var property in stringProps)
                property.SetMaxLength(128);
            //todo check if this is right set the string max length for all properties globally

            modelBuilder.Entity<User>().Property(u => u.PictureUrl).HasMaxLength(256);

            modelBuilder.Entity<User>().Property(u => u.Id).HasMaxLength(64);
            modelBuilder.Entity<UserPicture>().Property(u => u.UserId).HasMaxLength(64);

            modelBuilder.Entity<User>().Property(u => u.PictureUrl).HasMaxLength(512);

            //because facebook for example is very big
            modelBuilder.Entity<ProviderLink>().Property(id => id.Id).HasMaxLength(256);

            modelBuilder.Entity<ProviderLink>().Property(id => id.UserId).HasMaxLength(64);
        }

        private void IntListConversion(ModelBuilder modelBuilder)
        {
            Expression<Func<List<int>, List<int>, bool>> equalsExpression =
                (c1, c2) => c1.SequenceEqual(c2);
            Expression<Func<List<int>, int>> hashCodeExpression = hce =>
                hce.Aggregate((a, v) => HashCode.Combine(a, v.GetHashCode()));
            //this makes a hashcode to the list from it's elements
            //so 2 lists with the same values have the same hashcode

            var intListToStringComparer = new ValueComparer<List<int>>(
                equalsExpression,
                hashCodeExpression,
                c => c.ToList() //this is not used, because taking the snapshot doesn't have anything customized 
            );

            Expression<Func<List<int>, string>> serializeIntListExpression =
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null);
            Expression<Func<string, List<int>>> deserializeIntListExpression =
                v => JsonSerializer.Deserialize<List<int>>(v, (JsonSerializerOptions)null);

            var intListConverter = new ValueConverter<List<int>, string>(serializeIntListExpression,
                deserializeIntListExpression);

            modelBuilder.Entity<User>().Property(u => u.OwnedBackgroundIds)
                .HasConversion(intListConverter, intListToStringComparer);

            modelBuilder.Entity<User>().Property(u => u.OwnedCardBackIds)
                .HasConversion(intListConverter, intListToStringComparer);

            modelBuilder.Entity<User>().Property(u => u.OwnedTitleIds)
                .HasConversion(intListConverter, intListToStringComparer);
        }
        private void StringListConversion(ModelBuilder modelBuilder)
        {
            Expression<Func<List<string>, List<string>, bool>> equalsExpression =
                (c1, c2) => c1.SequenceEqual(c2);
            Expression<Func<List<string>, int>> hashCodeExpression = arr =>
                ((IStructuralEquatable)arr).GetHashCode(EqualityComparer<string>.Default);

            var stringListToStringComparer = new ValueComparer<List<string>>(
                equalsExpression,
                hashCodeExpression,
                c => c.ToList() //this is not used, because taking the snapshot doesn't have anything customized 
            );

            Expression<Func<List<string>, string>> serializeStringListExpression =
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null);
            Expression<Func<string, List<string>>> deserializeStringListExpression =
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null);

            var intListConverter = new ValueConverter<List<string>, string>(serializeStringListExpression,
                deserializeStringListExpression);

            // modelBuilder.Entity<User>().Property(u => u.OwnedPlayers)
            // .HasConversion(intListConverter, stringListToStringComparer);
        }
        private void StringStringDicConversion(ModelBuilder modelBuilder)
        {
            Expression<Func<Dictionary<string, string>, Dictionary<string, string>, bool>> equalsExpression =
                (c1, c2) => c1.SequenceEqual(c2);
            Expression<Func<Dictionary<string, string>, int>> hashCodeExpression =
                dic => JsonSerializer.Serialize(dic, (JsonSerializerOptions)null).GetHashCode();

            var comparer = new ValueComparer<Dictionary<string, string>>(
                equalsExpression,
                hashCodeExpression,
                c => c.ToDictionary(x => x.Key, x => x.Value)
                //this is not used, because taking the snapshot doesn't have anything customized 
            );


            Expression<Func<Dictionary<string, string>, string>> serializeExpression =
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null);
            Expression<Func<string, Dictionary<string, string>>> deserializeExpression =
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions)null);

            var converter = new ValueConverter<Dictionary<string, string>, string>(serializeExpression,
                deserializeExpression);

            // modelBuilder.Entity<User>().Property(u => u.SelectedPlayers)
            // .HasConversion(converter, comparer);
        }
    }
}