using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ConsoleApp1
{
    public class Player
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string PlayerString { get; set; } = string.Empty;
        public string PlayerPosition { get; set; } = string.Empty;
        public int TeamId { get; set; }
        public Team Team { get; set; } = null!;
    }

    public class Team
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public List<Player> Players { get; set; } = new List<Player>();
    }

    public class TeamMatches
    {
        public int Id { get; set; }
        [Required]
        public int Team1Id { get; set; }
        public Team Team1 { get; set; } = null!;
        [Required]
        public int Team2Id { get; set; }
        public Team Team2 { get; set; } = null!;
        public int Team1Score { get; set; }
        public int Team2Score { get; set; }
        public string Date { get; set; } = string.Empty;
        public List<Goal> Goals { get; set; } = new List<Goal>();
    }

    public class Goal
    {
        public int Id { get; set; }
        [Required]
        public int PlayerId { get; set; }
        public Player Player { get; set; } = null!;
        [Required]
        public int MatchId { get; set; }
        public TeamMatches Match { get; set; } = null!;
        public string TimeScored { get; set; } = string.Empty;
    }

    public class MyAppDbcontext : DbContext
    {
        public DbSet<Team> Teams { get; set; } = null!;
        public DbSet<Player> Players { get; set; } = null!;
        public DbSet<TeamMatches> Matches { get; set; } = null!;
        public DbSet<Goal> Goals { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string connectionString = @"Server=RaductionPc\Test;Database=Football;Trusted_Connection=True;TrustServerCertificate=True";
            optionsBuilder.UseSqlServer(connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TeamMatches>()
                .HasOne(m => m.Team1).WithMany().HasForeignKey(m => m.Team1Id).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<TeamMatches>()
                .HasOne(m => m.Team2).WithMany().HasForeignKey(m => m.Team2Id).OnDelete(DeleteBehavior.Restrict);
        }
    }

    class Program
    {
        static void AddMatchWithCheck(MyAppDbcontext db, string t1, string t2, string date, int s1, int s2)
        {
            var team1 = db.Teams.FirstOrDefault(m => m.Name == t1);
            var team2 = db.Teams.FirstOrDefault(m => m.Name == t2);

            if (team1 == null || team2 == null) return;

            bool exists = db.Matches.Any(m =>
                ((m.Team1Id == team1.Id && m.Team2Id == team2.Id) || (m.Team1Id == team2.Id && m.Team2Id == team1.Id))
                && m.Date == date);

            if (!exists)
            {
                db.Matches.Add(new TeamMatches { Team1Id = team1.Id, Team2Id = team2.Id, Date = date, Team1Score = s1, Team2Score = s2 });
                db.SaveChanges();
            }
        }

        static void ChangeInfo(MyAppDbcontext db, string t1, string t2, string oldDate, string newDate, int s1, int s2)
        {
            var match = db.Matches.FirstOrDefault(m =>
                (m.Team1.Name == t1 && m.Team2.Name == t2 || m.Team1.Name == t2 && m.Team2.Name == t1)
                && m.Date == oldDate);

            if (match != null)
            {
                match.Team1Score = s1;
                match.Team2Score = s2;
                match.Date = newDate;
                db.SaveChanges();
            }
        }

        static void DeleteMatchWithConfirm(MyAppDbcontext db, string t1, string t2, string date)
        {
            var match = db.Matches.FirstOrDefault(m =>
                (m.Team1.Name == t1 && m.Team2.Name == t2 || m.Team1.Name == t2 && m.Team2.Name == t1)
                && m.Date == date);

            if (match != null)
            {
                Console.WriteLine($"Delete match {t1} - {t2} ({date})? (y/n)");
                if (Console.ReadLine()?.ToLower() == "y")
                {
                    db.Matches.Remove(match);
                    db.SaveChanges();
                }
            }
        }

        static void ShowGoalDifference(MyAppDbcontext db)
        {
            var teams = db.Teams.ToList();
            foreach (var team in teams)
            {
                int scored1 = db.Matches.Where(m => m.Team1Id == team.Id).Sum(m => m.Team1Score);
                int missed1 = db.Matches.Where(m => m.Team1Id == team.Id).Sum(m => m.Team2Score);
                
                int scored2 = db.Matches.Where(m => m.Team2Id == team.Id).Sum(m => m.Team2Score);
                int missed2 = db.Matches.Where(m => m.Team2Id == team.Id).Sum(m => m.Team1Score);

                int totalScore = scored1 + scored2;
                int totalMissed = missed1 + missed2;

                Console.WriteLine(totalScore-totalMissed);
            }
        }

        static void ShowAllMatches(MyAppDbContext db)
        {
            var allMatches = db.Matches
                .Include(m => m.Team1)
                .Include(m => m.team2)
                .Include(m => m.Goals).ThenInclude(g => g.Player)
                .ToList();
            foreach (var m in matches)
            {
                Console.WriteLine($"[{m.Date}] {m.Team1.Name} {m.Team1Score} : {m.Team2Score} {m.Team2.Name}");
                if (m.Goals.Any())
                {
                    Console.WriteLine("  Голи:");
                    foreach (var g in m.Goals)
                        Console.WriteLine($"    - {g.Player.Name} ({g.TimeScored}')");
                }
            }
        }
        
        static void ShowAllMatches(MyAppDbContext db, string date)
        {
            var allMatches = db.Matches
                .Include(m => m.Team1)
                .Include(m => m.team2)
                .Where(m => date = m.Date)
                .ToList();
            foreach (var m in matches)
            {
                Console.WriteLine($"[{m.Date}] {m.Team1.Name} {m.Team1Score} : {m.Team2Score} {m.Team2.Name}");
            }
        }
        
        static void ShowAllMatches(MyAppDbContext db, string team)
        {
            var allMatches = db.Matches
                .Include(m => m.Team1)
                .Include(m => m.team2)
                .Where(m => m.Team1 == team || m.Team2 == team)
                .ToList();
            foreach (var m in matches)
            {
                Console.WriteLine($"[{m.Date}] {m.Team1.Name} {m.Team1Score} : {m.Team2Score} {m.Team2.Name}");
            }
        }
        
        static void ShowAllMatches(MyAppDbContext db, string date)
        {
            var allScores = db.Goals
                .Include(g => g.Player)
                .Include(g => g.Match)
                .Where(m => date = m.Date)
                .Select(g => g.Player.Name)
                .Distinct()
                .ToList();
            foreach (var m in matches)
            {
                Console.WriteLine($"[{m.Date}] {m.Team1.Name} {m.Team1Score} : {m.Team2Score} {m.Team2.Name}");
            }
        }
        

        static void Main(string[] args)
        {
            using (var db = new MyAppDbcontext())
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();

                var realMadrid = new Team { Name = "Real Madrid", Country = "Spain" };
                var barcelona = new Team { Name = "FC Barcelona", Country = "Spain" };
                db.Teams.AddRange(realMadrid, barcelona);
                db.SaveChanges();

                AddMatchWithCheck(db, "Real Madrid", "FC Barcelona", "2024-04-21", 2, 1);
                ChangeInfo(db, "Real Madrid", "FC Barcelona", "2024-04-21", "2024-04-22", 3, 3);
                DeleteMatchWithConfirm(db, "Real Madrid", "FC Barcelona", "2024-04-22");
            }
        }
    }
}