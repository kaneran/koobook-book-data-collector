using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace KoobookServiceConsoleApp.Amazon
{
    public class AmazonModel
    {
        public double? AverageRating {
            get;
            set;
        }

        public int? FiveStarRatingPercentage
        {
            get;
            set;
        }

        public int? FourStarRatingPercentage
        {
            get;
            set;
        }

        public int? ThreeStarRatingPercentage
        {
            get;
            set;
        }

        public int? TwoStarRatingPercentage
        {
            get;
            set;
        }

        public int? OneStarRatingPercentage
        {
            get;
            set;
        }

        public List<string> Reviews
        {
            get;
            set;
        }

        public int? ReviewCount
        {
            get;
            set;
        }

    }
}
