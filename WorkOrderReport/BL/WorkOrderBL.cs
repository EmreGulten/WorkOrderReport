using Microsoft.Data.SqlClient;
using WorkOrderReport.Models;

namespace WorkOrderReport.BL
{
    public static class WorkOrderBL
    {
        public static List<WorkOrderModel> GetWorkOrders(string connection)
        {
            List<WorkOrderModel> workOrderList = new();

            using (SqlConnection con = new SqlConnection(connection))
            {
                SqlCommand cmd = new();
                SqlDataReader dr;
                cmd.Connection = con;
                con.Open();
                cmd.CommandText = "select * from tblWorkOrders";
                dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    WorkOrderModel workOrder = new();
                    workOrder.Id = Convert.ToInt32(dr["id"]);
                    workOrder.Start = Convert.ToDateTime(dr["start"]);
                    workOrder.End = Convert.ToDateTime(dr["end"]);
                    workOrderList.Add(workOrder);
                }

                dr.Close();
                con.Close();
            }

            return workOrderList;
        }
    }
}
