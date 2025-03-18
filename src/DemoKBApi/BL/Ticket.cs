namespace DemoKBApi.BL
{
    public class Ticket
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }


    public class TicketRepo
    {
        //private List<Ticket> _ticketList;
        public List<Ticket> Tickets { get; private set; }
        private static TicketRepo instance = null;

        private TicketRepo()
        {
            Tickets = new List<Ticket>();
        }

        public static TicketRepo Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new TicketRepo();
                }
                return instance;
            }
        }
    }
}