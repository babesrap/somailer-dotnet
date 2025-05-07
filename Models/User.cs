using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace dotnetprojekt.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please enter the name")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Please enter the password")]
        public string Password { get; set; }
        [Required(ErrorMessage = "Please enter the email")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage ="Invalid email format")]
        public string Email { get; set; }
        
        // Navigation properties
        public ICollection<Rating> Ratings { get; set; }


        // relation to admin
        public virtual Admin Admin { get; set; }
    }
}