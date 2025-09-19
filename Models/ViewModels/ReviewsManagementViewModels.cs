using System.ComponentModel.DataAnnotations;
using StarTickets.Models;
using System.Collections.Generic;

namespace StarTickets.Models.ViewModels
{
	public class ReviewsManagementListViewModel
	{
		public List<EventRating> Ratings { get; set; } = new List<EventRating>();
		public string Search { get; set; } = string.Empty;
		public int? RatingFilter { get; set; }
		public bool? ApprovedFilter { get; set; }
		public int CurrentPage { get; set; }
		public int PageSize { get; set; }
		public int TotalItems { get; set; }
		public int TotalPages { get; set; }
	}
}
