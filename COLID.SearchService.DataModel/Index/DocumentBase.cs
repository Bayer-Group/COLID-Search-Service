using System;
using System.Collections.Generic;
using System.Text;
using COLID.SearchService.DataModel.Search;
using Microsoft.Identity.Client;

namespace COLID.SearchService.DataModel.Index
{
    public abstract class DocumentBase
    {
        public UpdateIndex Index { get; set; }

        protected DocumentBase()
        {
            Index = UpdateIndex.Published;
        }
    }
}
