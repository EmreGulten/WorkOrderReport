using System.Configuration;
using System.Data;
using WorkOrderReport.BL;
using WorkOrderReport.Models;

namespace WorkOrderReport
{
    public partial class Form1 : Form
    {
        private static readonly string connectionString = ConfigurationManager.ConnectionStrings["MainConnection"].ToString();
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            

        }

        private void btnRapor_Click(object sender, EventArgs e)
        {
            // veritaban�ndan i� emirleri ve duru� verileri al�n�r
            List<WorkOrderModel> workOrders = WorkOrderBL.GetWorkOrders(connectionString);
            List<StopoverModel> stopovers = StopoversBL.GetStopovers(connectionString);

            var reportItems = new Dictionary<int, Dictionary<string, int>>(); // raporlama i�lemi i�in reportItems adl� bir s�zl�k olu�turuyorum. Bu nesne her bir i� emri i�in duru� nedenleri ve bu nedenlere ait toplam s�relerin sakland��� bir s�zl�k olarak kullan�lacak.

            var reasons = new List<string>(); // duru�lar�n nedenlerini tutmak i�in bir liste 

            foreach (var stopover in stopovers)
            {
                if (!reasons.Contains(stopover.Reason)) // listesinde yoksa, reasons listesine eklenir.
                {
                    reasons.Add(stopover.Reason); // Duru� nedenleri bir listeye ekliyorum.
                    /* Bu d�ng� stopovers listesindeki her bir duru� verisi i�in d�ner ve her bir duru� verisinin nedenini reasons listesine ekler. 
                     * Bu ileride raporlama i�lemi i�in kullanaca��m olan duru� nedenlerinin bir listesini olu�turur. */
                }

                //E�le�en i� emirleri ve duru� nedenleri i�in raporlama verileri olu�turulur Bu kod blo�u her bir duru� verisi i�in e�le�en i� emirlerini bulur ve her bir i� emri i�in duru� nedenlerine g�re raporlama verileri olu�turur. �ncelikle, her bir i� emri ve duru� verisi aras�ndaki zaman �ak

                var matchedOrders = workOrders.Where(o => stopover.Start < o.End && o.Start < stopover.End).ToArray(); // duru�un tarih aral���na uyan i� emirlerini bulup ve matchedOrders adl� bir diziye at�yorum

                foreach (var item in matchedOrders) // �rt��en her i� emri i�in bir d�ng� ba�lat�l�r
                {
                    var defaultEndDate = stopover.End;
                    var defaultStartDate = stopover.Start;
                    /* birden fazla kullanaca��m i�in duru�un ba�lang�� ve biti� tarihlerini de�i�kene al�yorum kar���kl�k olmas�n diye */

                    if (item.Start > stopover.Start) //  i� emrinin ba�lang�� tarihi duru�un ba�lang�� tarihinden b�y�ks, defaultStartDate de�i�kenine i� emrinin ba�lang�� tarihi atan�r.
                        defaultStartDate = item.Start;

                    if (item.End < stopover.End) // i� emrinin biti� tarihi duru�un biti� tarihinden k���kse defaultEndDate de�i�kenine i� emrinin biti� tarihi atan�r.
                        defaultEndDate = item.End;

                    var totalStopoverMinutes = (int)defaultEndDate.Subtract(defaultStartDate).TotalMinutes; // dakika olarak duru�un �rt��t��� s�re hesaplan�r ve totalStopoverMinutes de�i�kenine atan�r.

                    if (reportItems.ContainsKey(item.Id)) // Bu sat�r, i� emri olup olmad���n� kontrol eder. Bu kontrol, daha �nce eklendi mi yoksa ekleme yap�lmas� m� gerekti�ini belirlemek i�in yap�l�r.
                    {
                        var items = reportItems[item.Id];
                        var existsData = items.FirstOrDefault(i => i.Key == stopover.Reason); //  bir �stte  duru� nedenleri ile e�le�en bir anahtar�n var olup olmad���n� kontrol eder. E�er varsa, bu anahtar ve de�erini existsData de�i�keninde saklar.
                        if (!existsData.Equals(default(KeyValuePair<string, int>)))  // de�i�keninin bo� olup olmad���n� kontrol eder.
                        {
                            items.Remove(existsData.Key); // E�er bu de�er zaten varsa, bu blok i�indeki kod, bu anahtar� kullanarak s�zl�kteki de�eri g�nceller.
                            items.Add(stopover.Reason, existsData.Value + totalStopoverMinutes);
                            reportItems[item.Id] = items;
                            continue;
                        }

                        reportItems[item.Id].Add(stopover.Reason, totalStopoverMinutes);
                        continue;
                    }

                    reportItems.Add(item.Id, new Dictionary<string, int>() {
                        { stopover.Reason, totalStopoverMinutes }
                    });

                }
            }



            var dt = new DataTable();
            var columns = reasons.ToList(); // duru� nedenlerini al�yorum kolonlara da��tmak i�in.

            //Birden fazla kullan�ld��� i�in de�i�kene ald�m.
            var orderFieldName = "�� Emri";
            var totalFieldName = "Toplam";

            columns.Insert(0, orderFieldName);
            columns.Add(totalFieldName);

            foreach (var col in columns) // duru� nedenlerini kolonlara al�yorum.
                dt.Columns.Add(col);

            //freeOrders adl� bir liste olu�turarak herhangi bir duru�a girmemi� i� emirlerini belirliyorum
            var freeOrders = workOrders.Select(o => o.Id).ToList().Except(reportItems.Select(rp => rp.Key).ToList()).ToList();

            //herhangi bir duru�a girmemi� i� emirlerini  reportItems s�zl���ne eklenmesi gerekiyor. Bunu a�a��da yap�yorum.
            foreach (var fo in freeOrders)
                reportItems.Add(fo, new Dictionary<string, int>() { });

            foreach (var item in reportItems)
            {
                var row = dt.NewRow(); // her bir ��e i�in dt tablosuna bir sat�r ekliyorum.
                foreach (var col in columns)
                    row[col] = "...";  // sat�r�n her bir h�cresine varsay�lan olarak "..." de�eri atan�r.

                row[0] = item.Key; // i� emri s�tununa mevcut i� emri id sini ekliyorum.
                var total = 0;
                foreach (var reason in item.Value)
                {
                    row[reason.Key] = reason.Value;
                    total += reason.Value;
                }

                row[totalFieldName] = total;
                dt.Rows.Add(row);
            }


            // toplam de�erlerin yaz�laca�� son sat�r 
            var totalRow = dt.NewRow();
            totalRow[orderFieldName] = totalFieldName;
            foreach (var reason in reasons)
                totalRow[reason] = dt.AsEnumerable().Sum(r => r[reason].ToString() == "..." ? 0 : int.Parse(r[reason].ToString()));

            //E�er ... var ise s�f�r olarak alal�m
            totalRow[totalFieldName] = dt.AsEnumerable().Sum(r => r[totalFieldName].ToString() == "..." ? 0 : int.Parse(r[totalFieldName].ToString()));
            dt.Rows.Add(totalRow);

            dtGrid.DataSource = dt;

            //Kar���k ekledi�imiz i� emirlerini s�ralayal�m
            dtGrid.Sort(dtGrid.Columns[orderFieldName], System.ComponentModel.ListSortDirection.Ascending);
        }
    }
}