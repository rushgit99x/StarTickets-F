namespace StarTickets.Models.ViewModels
{
    public class HomeViewModel
    {
        public List<Event> FeaturedEvents { get; set; } = new List<Event>();
        public List<Event> ThisWeekEvents { get; set; } = new List<Event>();
        public List<Event> ThisMonthEvents { get; set; } = new List<Event>();
        public List<Event> NextMonthEvents { get; set; } = new List<Event>();
        public List<EventCategory> Categories { get; set; } = new List<EventCategory>();
        public List<Venue> Venues { get; set; } = new List<Venue>();
        public bool IsAuthenticated { get; set; }
    }
}
