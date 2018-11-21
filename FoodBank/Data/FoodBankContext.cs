using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace FoodBank.Models
{
    public class FoodBankContext : DbContext
    {
        public FoodBankContext (DbContextOptions<FoodBankContext> options)
            : base(options)
        {
        }

        public DbSet<FoodBank.Models.FoodItem> FoodItem { get; set; }
    }
}
