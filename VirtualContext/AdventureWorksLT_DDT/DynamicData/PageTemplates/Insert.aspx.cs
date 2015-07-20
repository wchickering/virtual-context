using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Xml.Linq;
using System.Web.DynamicData;
using Schrodinger.Diagnostics;
using Schrodinger.LinqInterceptor;

namespace AdventureWorksLT
{
    public partial class Insert : System.Web.UI.Page
    {
        protected MetaTable table;
        protected LinqInterceptor<AdventureWorksLTDataContext> _linqInterceptor;

        protected void Page_Init(object sender, EventArgs e)
        {
            DynamicDataManager1.RegisterControl(DetailsView1);
            _linqInterceptor = new LinqInterceptor<AdventureWorksLTDataContext>(DetailsDataSource, DetailsDataSource.GetTable().EntityType);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            table = DetailsDataSource.GetTable();
            Title = table.DisplayName;
        }

        protected void DetailsView1_ItemCommand(object sender, DetailsViewCommandEventArgs e)
        {
            if (e.CommandName == DataControlCommands.CancelCommandName)
            {
                Response.Redirect(table.ListActionPath);
            }
        }

        protected void DetailsView1_ItemInserted(object sender, DetailsViewInsertedEventArgs e)
        {
            if (e.Exception == null || e.ExceptionHandled)
            {
                Response.Redirect(table.ListActionPath);
            }
        }

        //protected void DetailsDataSource_ContextCreated(object sender, LinqDataSourceStatusEventArgs e)
        //{
        //    AdventureWorksLTDataContext context = e.Result as AdventureWorksLTDataContext;
        //    if (context == null)
        //    {
        //        throw new InvalidOperationException("Failed to cast DataContext.");
        //    }
        //    context.Log = new DebuggerWriter();
        //}
    }
}
