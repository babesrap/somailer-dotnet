using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace dotnetprojekt.Models
{
    public class Rating
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int WineId { get; set; }
        
        [Column(TypeName = "decimal(3,1)")]
        public decimal RatingValue { get; set; }
        public DateTime Date { get; set; }
        
        // Navigation properties
        public User User { get; set; }
        public Wine Wine { get; set; }
    }
}