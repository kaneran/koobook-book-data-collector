using System;
using System.Collections.Generic;

namespace KoobookServiceConsoleApp.GoolgeBooksApi
{
    public class GoogleBookModel
    {

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

        public string Subtitle
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

        public List<string> Genres {
            get;
            set;
        }

        public string ThumbnailUrl
        {
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
