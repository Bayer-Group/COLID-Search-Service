using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COLID.SearchService.DataModel.DTO
{
    public class Carrot2RequestDTO
    {
        public string language { get; set; }
        public string algorithm { get; set; }        
        public preprocessingParameters parameters { get; set; }
        public IList<IDictionary<string, string>> documents { get; set; }

        public Carrot2RequestDTO()
        {
            language = "English";
            algorithm = "Lingo";
            documents = new List<IDictionary<string, string>>();
            parameters = new preprocessingParameters();
        }
    }

    public class preprocessingParameters
    {
        public Params preprocessing { get; set; } 
        public preprocessingParameters()
        {
            preprocessing = new Params();
        }
    }

    public class Params
    {
        public int phraseDfThreshold { get; set; }
        public int wordDfThreshold { get; set; }
        public Params()
        {
            phraseDfThreshold = 1;
            wordDfThreshold = 1;
        }
    }
}
