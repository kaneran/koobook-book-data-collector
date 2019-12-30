using System;
using System.Collections.Generic;

namespace KoobookServiceConsoleApp.GoolgeBooksApi
{
    public class BookModel
    {
        public string Id
        {
            get;
            set;
        }

        public string Title
        {
            get;
            set;
        }

        public string Description
        {
            get;
            set;
        }

        public int? PageCount
        {
            get;
            set;
        }

        public double? AverageRating
        {
            get;
            set;
        }

        public List<string> Authors
        {
            get;
            set;
        }

    }

}
