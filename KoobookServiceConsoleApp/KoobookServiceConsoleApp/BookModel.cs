using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoobookServiceConsoleApp
{
    public class BookModel
    {
        public string Title {
            get;
            set;
        }
        public List<string> Genres
        {
            get;
            set;
        }

        public List<string> Authors
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

        public string Isbn
        {
            get;
            set;
        }

        public double? GoodreadsAverageRating
        {
            get;
            set;
        }

        public double? GoogleBooksAverageRating
        {
            get;
            set;
        }

        public double? AmazonAverageRating
        {
            get;
            set;
        }

        public int? AmazonFiveStarRatingPercentage
        {
            get;
            set;
        }

        public int? AmazonFourStarRatingPercentage
        {
            get;
            set;
        }

        public int? AmazonThreeStarRatingPercentage
        {
            get;
            set;
        }

        public int? AmazonTwoStarRatingPercentage
        {
            get;
            set;
        }

        public int? AmazonOneStarRatingPercentage
        {
            get;
            set;
        }

        public List<string> AmazonReviews
        {
            get;
            set;
        }

        public int? AmazonReviewsCount
        {
            get;
            set;
        }




    }
}
