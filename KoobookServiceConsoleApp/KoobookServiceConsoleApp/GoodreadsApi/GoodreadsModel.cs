using Goodreads.Models.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoobookServiceConsoleApp.GoodReadsApi
{
    public class GoodreadsModel
    {

        public string Title {
            get;
            set;
        }

        public string Isbn {
            get;
            set;
        }

        public string Description {
            get;
            set;
        }

        public double? AverageRating {
            get;
            set;
        }

        public IReadOnlyList<AuthorSummary> Authors {
            get;
            set;
        }

        
        public int? PageCount
        {
            get;
            set;
        }
    }
}
