using System.ComponentModel.DataAnnotations;

namespace StarTickets.Models.ViewModels
{
    public class AboutViewModel
    {
        public string CompanyName { get; set; } = "StarEvents Pvt Ltd";
        public string FoundedYear { get; set; } = "2025";
        public int TotalCustomers { get; set; } = 500000;
        public int TotalEvents { get; set; } = 10000;
        public int PartnerVenues { get; set; } = 250;
        public int CitiesCovered { get; set; } = 50;

        // Add other properties as needed for dynamic content
        public List<TeamMember>? TeamMembers { get; set; }
        public List<TimelineEvent>? CompanyTimeline { get; set; }
    }

    public class TeamMember
    {
        public string Name { get; set; } = "";
        public string Role { get; set; } = "";
        public string Bio { get; set; } = "";
        public string ImageUrl { get; set; } = "";
    }

    public class TimelineEvent
    {
        public int Year { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
    }
}