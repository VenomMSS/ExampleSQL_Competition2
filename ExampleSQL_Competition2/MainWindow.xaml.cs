using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data.SQLite;
using Microsoft.Win32;
using System.IO;
using System.Data;
using System.Collections;

namespace ExampleSQL_Competition2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static String table_rally = "rally";
        private static String table_competitor = "competitors";
        private static String table_checkpoint = "checkpoints";
        private static String table_stage = "stages";
        private static String table_timing = "timings";
        private static String table_scores = "scores";

        // fileds in the rally table
        private static String field_EventName = "EventName";
        private static String field_EventDate = "Date";
        private static String field_Starttime = "Starttime";
        private static String field_Together = "startTogether";
        private static String field_interval = "startInterval";
        private static String field_timeout = "MaxAllowedTime";
        private static String field_out_of_time_penalty = "OutOfTimePenalty";
        private static String field_missed_penalty = "MissedPenalty";

        // fields in the competitors table
        private static String field_compNo = "compNumber";
        private static String field_CompName = "Competitor_Name";
        private static String field_Machine = "Make_Model";
        private static String field_vehicle_type = "Vehicle_Type";
        private static String field_vehicle_class = "Class";
        private static String field_vehicle_size = "Size";
        
        // fields in the timings table
        private static String field_location = "Location";
        private static String field_TimeKeeper = "TK_Initials";
        private static String field_file_version = "Version";
        private static String field_time = "timed";
        private static String field_compFK = "competitorFK";
        private static String field_chckptFK = "checkpointFK";

        // fields in the stages table
        private static String field_stageName = "StageName";
        private static String field_ordinal = "StageOrd";
        private static String field_units = "DistanceUnits";
        private static String field_distance = "Distance";
        private static String field_speed = "Speed";
        private static String field_Breaks = "Breaks_mins";
        private static String field_begin = "StartcheckptFK";
        private static String field_end = "EndcheckptFK";

        // fields in the score table
        private static String field_competitor = "CompetitorFK";
        private static String field_stage = "StageFK";
        private static String field_timetaken = "timetaken";
        private static String field_score = "points";


        OpenFileDialog OFileDialog;
        SQLiteConnection dataBase;
        SQLiteDataAdapter rallyAdapter, compAdapter, checkptAdapter, stageAdapter, timingAdapter, foundAdapter;
        String errorlog;
        DataSet ds_competitors, ds_checkpoints, ds_timings, ds_stages, ds_rally;
        DataTable dt_stages;


        public MainWindow()
        {
            String connectionString;
            // SQLiteCommand SQL_cmd;
            InitializeComponent();

            // version number in connection string  is the SQLite version and needs to be set to 3.
            connectionString = "Data Source =c:\\Databases\\XYZcompetitionNEW3.db;Version=3;New=True;Compress=True;";
            dataBase = new SQLiteConnection(connectionString);
            dataBase.Open();
           
            createAllTables();
            // dataBase.Close();
            errorlog = "";

        }

        private void setupfilebutton_Click(object sender, RoutedEventArgs e)
        {
            OFileDialog = new OpenFileDialog();
            OFileDialog.Title = "Open setup File";
           
            OFileDialog.FileOk += OFileDialog_FileOk;
            OFileDialog.ShowDialog();

        }

        private void timeFileButton_Click(object sender, RoutedEventArgs e)
        {
            OFileDialog = new OpenFileDialog();
            OFileDialog.Title = "Open a timing file";

            OFileDialog.FileOk += OFileDialog_FileOk1;
            OFileDialog.ShowDialog();
        }

        // open and read data from timing file    
        private void OFileDialog_FileOk1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // read timing file;
            SQLiteCommand SQL_cmd;
            String commandString;
            String name_id, loc_id, when;
            string fullpathname = OFileDialog.FileName;
            FileInfo scr = new FileInfo(fullpathname);
            //
            FilelistBox.Items.Clear();
            TextReader reader = scr.OpenText();
            string line = reader.ReadLine();
            if (line != null)
            {
                // dataBase.Open();

                while (line != null)
                {
                    FilelistBox.Items.Add(line);
                    String[] field = line.Split(',');
                    // saveName adds name(actually competition number) to Competitor table if new
                    name_id = saveName(field[2], field[1], field[5], field[6]); // name_id return as compID from table
                    // saveCheckpoint adds checkpoint to Checkpoint table if new
                    loc_id = saveCheckpoint(field[3]);
                    when = field[4].Replace('/', '-');
                    try
                    {
                        SQL_cmd = dataBase.CreateCommand();
                        commandString = "INSERT INTO " + table_timing +
                            " (" + field_compFK + ", " + field_chckptFK + ", " + field_time + ", " + field_TimeKeeper + 
                            ", " + field_file_version + ") VALUES ('" +
                                  name_id + "', '" + loc_id + "', '" + when + "', '" + field[7] + "', '" + field[0] + "' );";
                        SQL_cmd.CommandText = commandString;
                        SQL_cmd.ExecuteNonQuery();
                        // this adds FK for comp number and location, and the time to timing table
                    }
                    catch (Exception)
                    {
                        errorlog = errorlog + line;
                        SearchlistBox.Items.Add(line);
                    }
                    line = reader.ReadLine();
                }
                // dataBase.Close();
            }
            else
            {
                SearchlistBox.Items.Add("This file is empty");
            }
         }

        private string saveName(String compNumber, String VType, String VClass, String VSize)
        {
            String commandString, foundrow;
            SQLiteCommand Sqlcmd;

            // check if this is known
            commandString = "SELECT * FROM " + table_competitor + " WHERE " +
                field_compNo + " = " + compNumber + "; ";
            foundrow = null;
            Sqlcmd = dataBase.CreateCommand();
            Sqlcmd.CommandText = commandString;
            SQLiteDataReader datareader = Sqlcmd.ExecuteReader();
            if (datareader.HasRows)
            {
                // record found for this compnumber
                while (datareader.Read())
                {
                    foundrow = datareader["compID"].ToString();
                }
            }
            else
            {
                // no record of this comp number
                // therefore insert new record into competitors table
                Sqlcmd = dataBase.CreateCommand();
                commandString = "INSERT INTO " + table_competitor + " (" +
                    field_compNo +", " + field_vehicle_type + ", " + field_vehicle_class + 
                    ", " + field_vehicle_size + ") VALUES ( '" + compNumber + "', '" + VType +
                    "', '" + VClass + "', '" + VSize + "' );";
                Sqlcmd.CommandText = commandString;
                Sqlcmd.ExecuteNonQuery();
                // now need to search again to get rowid
                Sqlcmd = dataBase.CreateCommand();
                commandString = "SELECT * FROM " + table_competitor + " WHERE " +
                field_compNo + " = " + compNumber + "; ";
                Sqlcmd.CommandText = commandString;
                datareader = Sqlcmd.ExecuteReader();
                while (datareader.Read())
                {
                    foundrow = datareader["compID"].ToString();
                }
            }
            return foundrow;
            
        }

        private String saveCheckpoint(String location)
        {
            String commandString, foundrow;
            SQLiteCommand Sqlcmd;

            // check if this is known
            commandString = "SELECT * FROM " + table_checkpoint + " WHERE " +
                field_location + " = '" + location + "'; ";
            foundrow = null;
            Sqlcmd = dataBase.CreateCommand();
            Sqlcmd.CommandText = commandString;
            SQLiteDataReader datareader = Sqlcmd.ExecuteReader();
            if (datareader.HasRows)
            {
                // record found for this compnumber
                while (datareader.Read())
                {
                    foundrow = datareader["cpID"].ToString();
                }
            }
            else
            {
                // no record of this checkpoint
                // therefore insert new record into checkpoint table
                Sqlcmd = dataBase.CreateCommand();
                commandString = "INSERT INTO " + table_checkpoint + " (" +
                    field_location + ") VALUES ( '" + location + "' );";
                Sqlcmd.CommandText = commandString;
                Sqlcmd.ExecuteNonQuery();
                // now need to search again to get rowid
                Sqlcmd = dataBase.CreateCommand();
                commandString = "SELECT * FROM " + table_checkpoint + " WHERE " +
                field_location + " = '" + location + "'; ";
                Sqlcmd.CommandText = commandString;
                datareader = Sqlcmd.ExecuteReader();
                while (datareader.Read())
                {
                    foundrow = datareader["cpID"].ToString();
                }
            }
            return foundrow;

        }

        private void restButton_Click(object sender, RoutedEventArgs e)
        {
            // Drop tables and recreate tables.
            // dataBase.Open();
            dropAllTables();
            createAllTables();
            // dataBase.Close();
        }

        private void dropAllTables()
        {
            SQLiteCommand sqlCmd;
            sqlCmd = dataBase.CreateCommand();
            sqlCmd.CommandText = "DROP TABLE " + table_rally;
            sqlCmd.ExecuteNonQuery();

            sqlCmd = dataBase.CreateCommand();
            sqlCmd.CommandText = "DROP TABLE " + table_stage;
            sqlCmd.ExecuteNonQuery();

            sqlCmd = dataBase.CreateCommand();
            sqlCmd.CommandText = "DROP TABLE " + table_checkpoint;
            sqlCmd.ExecuteNonQuery();

            sqlCmd = dataBase.CreateCommand();
            sqlCmd.CommandText = "DROP TABLE " + table_competitor;
            sqlCmd.ExecuteNonQuery();

            sqlCmd = dataBase.CreateCommand();
            sqlCmd.CommandText = "DROP TABLE " + table_timing;
            sqlCmd.ExecuteNonQuery();

            sqlCmd = dataBase.CreateCommand();
            sqlCmd.CommandText = "DROP TABLE " + table_scores;
            sqlCmd.ExecuteNonQuery();

        }

        private void createAllTables()
        {
            // this method is called only when database is opened.
            // called from constructor and from restbutton

            SQLiteCommand SQL_cmd;
            SQL_cmd = dataBase.CreateCommand();
            SQL_cmd.CommandText = "CREATE TABLE IF NOT EXISTS " + table_rally +
                " (rallyID integer primary key, " + field_EventName + " TEXT, " +
                    field_EventDate + " TEXT, " + field_Starttime + " TEXT, " +
                    field_interval + " INTEGER, " + field_Together + " INTEGER, "+
                    field_timeout + " INTEGER, " + field_out_of_time_penalty + " INTEGER, " + 
                    field_missed_penalty + " INTEGER);";
            SQL_cmd.ExecuteNonQuery();
            // create competitor table
             SQL_cmd.CommandText = "CREATE TABLE IF NOT EXISTS " + table_competitor +
                " (compID integer primary key, " + field_compNo + " TEXT, " +
                   field_CompName + " TEXT, " + field_Machine + " TEXT, " +
                   field_vehicle_type + " TEXT, " + field_vehicle_class + " TEXT, " + 
                   field_vehicle_size + " TEXT);";
            SQL_cmd.ExecuteNonQuery();
            SQL_cmd.CommandText = "CREATE TABLE IF NOT EXISTS " + table_checkpoint +
                " (cpID integer primary key, " + field_location + " TEXT);";
            SQL_cmd.ExecuteNonQuery();
            SQL_cmd.CommandText = "CREATE TABLE IF NOT EXISTS " + table_stage +
               " (stID integer primary key, " + field_stageName + " TEXT, " +
                field_units + " TEXT, " +  field_distance + " INTEGER, " + 
                field_speed + " INTEGER, " + field_Breaks + " INTEGER, " +
                field_begin + " INTEGER, " + field_end + " INTEGER);";
            SQL_cmd.ExecuteNonQuery();
            SQL_cmd.CommandText = "CREATE TABLE IF NOT EXISTS " + table_timing +
               " (_ID integer primary key, " + field_compFK + " INTEGER, " +
                field_chckptFK + " INTEGER, " + field_time + " TEXT, " +
                field_TimeKeeper + " TEXT, " + field_file_version +" TEXT);";
            SQL_cmd.ExecuteNonQuery();
            SQL_cmd.CommandText = "CREATE TABLE IF NOT EXISTS " + table_scores +
               " (scID integer primary key, " + field_competitor + " INTEGER, " +
                field_stage + " INTEGER, " + field_timetaken + " TEXT, " +
                field_score + " INTEGER);";
            SQL_cmd.ExecuteNonQuery();
        }

        private void editStageButton_Click(object sender, RoutedEventArgs e)
        {
            // Edit This method links a stage to the start and end checkpoints for the stage
            // call edit stage dialog if a stage is selected

            SQLiteCommand sqlCmd;
            String comString;
            SQLiteDataReader datareader;
            string stagename, foundrow;
            int stindex,  record;
            ArrayList   checkpoints;
            List<String> checkpointlist;
            // String cp;
            if (StagedataGrid.SelectedItem != null)
            {
                stindex= StagedataGrid.SelectedIndex;
                DataRow[] rows = dt_stages.Select();
                if (rows != null)
                {
                    DataRow row = rows[stindex];

                    stagename = "stage: " + row[1];
                    // name is in column 1 of the stage row
                }
                else {
                    stagename = "nothing";
                }
                
                // read checkpoints from the database 
                checkpoints = new ArrayList();
                checkpointlist = new List<string>();
                checkpointlist = get_checkpoints();
                foreach(string s in checkpointlist)
                          checkpoints.Add(s);

                
                EditStageDialog editdlg = new EditStageDialog(stagename, checkpoints);
                if (editdlg.ShowDialog() == true)
                {
                    // success// save returned data
                    string[] CPs = editdlg.Answer.Split(',');
                    beginlabel.Content = CPs[0];
                    finishlabel.Content = CPs[1];
                    DataRow row = rows[stindex];
                    row[6] = 1;
                    row[7] = 2;
                    SearchlistBox.Items.Add("stage " + row[1] + row[6]+ row[7]);
                    SearchlistBox.Items.Add("Begin " + CPs[0]);
                    SearchlistBox.Items.Add("End " + CPs[1]);

                    // get position of checkpoint in database
                    sqlCmd = dataBase.CreateCommand();
                    comString = "SELECT * FROM " + table_checkpoint + " WHERE " +
                              field_location + " = '" + CPs[0] + "'; ";
                    sqlCmd.CommandText = comString;
                    datareader = sqlCmd.ExecuteReader();
                    foundrow = "99";
                    while (datareader.Read())
                    {
                        foundrow = datareader["cpID"].ToString();
                    }
                    row[6] = int.Parse(foundrow);

                    // get end checkpoint
                    sqlCmd = dataBase.CreateCommand();
                    comString = "SELECT * FROM " + table_checkpoint + " WHERE " +
                              field_location + " = '" + CPs[1] + "'; ";
                    sqlCmd.CommandText = comString;
                    datareader = sqlCmd.ExecuteReader();
                    foundrow = "99";
                    while (datareader.Read())
                    {
                        foundrow = datareader["cpID"].ToString();
                    }
                    row[7] = int.Parse(foundrow);

                    // now update table_stage
                    sqlCmd = dataBase.CreateCommand();
                    comString = "UPDATE stages SET StartcheckptFK = '"+row[6]+
                        "' , EndcheckptFK = '"+ row[7]+"' WHERE StageName  = '" + row[1] + "'";
                    sqlCmd.CommandText = comString;
                    record = sqlCmd.ExecuteNonQuery();
                    SearchlistBox.Items.Add("Updated stage records " + record);
                    // these values need to be updated in the stage selected in the list
                    // SELECT cpID dt_checkpoint where cpname = CPs[0];
                    // SELECT cpID dt_checkpoint where cpname = CPs[1];

                    //rows[stindex][5] = beginindex;
                    //rows[stindex][6] = endindex;
                    // assume use UPDATE table_stage WHERE field_stagename = stagename 
                    // field_begin = index CP[0] field_end = index of CP[1] ??
                }

            }

        }

        private void SearchHandler(object sender, RoutedEventArgs e)
        {
            // callered by search button
            // firstly check if the searchfor text box has an entry
            if (searcefor.Text != "")
            {
                String comString;
                SQLiteCommand sqlCmd;
                String findString = searcefor.Text;
                // assume database is open
                // the field_compFK links to the row number in the competitors table
                // if you want to retrieve the competition number then need to search 
                // for the competitin number on the table_competitors first. (field_compNo)



                // build command and string
                sqlCmd = dataBase.CreateCommand();
                comString = "SELECT * FROM " + table_timing + " WHERE " + field_compFK + " = " + findString + ";";
                sqlCmd.CommandText = comString;

                // execute the sqlcommand to return a datagrid
                foundAdapter = new SQLiteDataAdapter(sqlCmd);
                DataSet foundDataset = new DataSet();
                foundAdapter.Fill(foundDataset, "My Table");
                foundDataGrid.ItemsSource = foundDataset.CreateDataReader();
                // this should populate data found into the founddatagrid


            }

        }

        private List<String> get_checkpoints()
        {
            List<String> entries = new List<string>();
            SQLiteCommand sqlCmd = dataBase.CreateCommand();
            String comString = "SELECT * FROM " + table_checkpoint;
            sqlCmd.CommandText = comString;
            SQLiteDataReader query;

           //  dataBase.Open();

            try
            {
                query = sqlCmd.ExecuteReader();

            }
            catch (SQLiteException error)
            {
               // dataBase.Close();
                return entries;
                //throw;
            }
            while (query.Read())
            {
                entries.Add(query.GetString(1));
            }
           // dataBase.Close();
            return entries;
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            // update tables to database
            dataBase.Close();
            this.Close();
        
        }

        private void loadfromDB()
        {
               // display all the entries in each table
                String comString;
                SQLiteCommand sqlCmd;


                //  dataBase.Open();
                // check if database is open?

                // competitor 
                sqlCmd = dataBase.CreateCommand();
                comString = "SELECT * FROM " + table_competitor;
                sqlCmd.CommandText = comString;
                compAdapter = new SQLiteDataAdapter(sqlCmd);
                ds_competitors = new DataSet();
                compAdapter.Fill(ds_competitors, "compTable");
                CompetitordataGrid.ItemsSource = ds_competitors.CreateDataReader();

                // show checkpoints
                sqlCmd = dataBase.CreateCommand();
                comString = "SELECT * FROM " + table_checkpoint;
                sqlCmd.CommandText = comString;
                checkptAdapter = new SQLiteDataAdapter(sqlCmd);
                ds_checkpoints = new DataSet();
                checkptAdapter.Fill(ds_checkpoints, "cpTable");
                CheckpointdataGrid.ItemsSource = ds_checkpoints.CreateDataReader();

                // stages
                //sqlCmd = dataBase.CreateCommand();
                //comString = "SELECT * FROM " + table_stage;
                //sqlCmd.CommandText = comString;
                //stageAdapter = new SQLiteDataAdapter(sqlCmd);
                // DataSet ds_stages = new DataSet();
                // stageAdapter.Fill(ds_stages, "stageTable");
                //StagedataGrid.ItemsSource = ds_stages.CreateDataReader();

                // alternatice stages using datatable
                sqlCmd = dataBase.CreateCommand();
                comString = "SELECT * FROM " + table_stage;
                sqlCmd.CommandText = comString;
                stageAdapter = new SQLiteDataAdapter(sqlCmd);
                dt_stages = new DataTable("Stages");
                stageAdapter.Fill(dt_stages);
                StagedataGrid.ItemsSource = dt_stages.DefaultView;

                // timings
                sqlCmd = dataBase.CreateCommand();
                comString = "SELECT * FROM " + table_timing;
                sqlCmd.CommandText = comString;
                timingAdapter = new SQLiteDataAdapter(sqlCmd);
                ds_timings = new DataSet();
                timingAdapter.Fill(ds_timings, "timingTable");
                TimingdataGrid.ItemsSource = ds_timings.CreateDataReader();

                // rally
                sqlCmd = dataBase.CreateCommand();
                comString = "SELECT * FROM " + table_rally;
                sqlCmd.CommandText = comString;
                rallyAdapter = new SQLiteDataAdapter(sqlCmd);
                ds_rally = new DataSet();
                rallyAdapter.Fill(ds_rally, "rallyTable");
                RallydataGrid.ItemsSource = ds_rally.CreateDataReader();

                
            }

        private void readButton_Click(object sender, RoutedEventArgs e)
        {

            loadfromDB();

           
        }

        private void OFileDialog_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Read setup file
            string fullpathname = OFileDialog.FileName;
            FileInfo scr = new FileInfo(fullpathname);
            //
            FilelistBox.Items.Clear();
            TextReader reader = scr.OpenText();
            // read first line with rally name etc
            string line = reader.ReadLine();
            if (line != null )
            {
                // dataBase.Open();
                addrallydetails(line);
                FilelistBox.Items.Add(line);

                // get next lines. These are stage details
                line = reader.ReadLine();
                while (line != null)
                {
                    FilelistBox.Items.Add(line);
                    addstagedetails(line);
                    line = reader.ReadLine();
                }

            }
           //  dataBase.Close();
        }

        private void addrallydetails(string details)
        {
            SQLiteCommand SQL_cmd;
            String commandString;
            // split details into relevent fields
            string[] field = details.Split(',');
            // should do check here should replace any current values
            SQL_cmd = dataBase.CreateCommand();
            commandString = "INSERT INTO " + table_rally + " (" +
                field_EventName + ", " + field_EventDate + ", " + field_Starttime +", "+
                field_interval + ", " + field_Together + ", " + field_timeout + ", " + 
                field_out_of_time_penalty + ", " + field_missed_penalty + 
                ") VALUES ('" +  field[0] + "', '" + field[1] + "' , '" + field[2] + "' , '" +
                field[3] + "' , '" + field[4] + "' , '" + field[5] + "' , '" + 
                field[6] + "' , '" + field[7] + "' );";
            SQL_cmd.CommandText = commandString;
            SQL_cmd.ExecuteNonQuery();
        }

        private void addstagedetails(string details)
        {
            SQLiteCommand SQL_cmd;
            String commandString;
            // split details into relevent fields
            string[] field = details.Split(',');
            // should check if stage is already added. use insert or replace
            SQL_cmd = dataBase.CreateCommand();
            commandString = "INSERT INTO " + table_stage + " (" +
                field_stageName + ", " + field_units + ", " + field_distance + ", " + field_speed + ", " + field_Breaks +
                 ") VALUES ('" +
                field[1] + "', '" + field[2] + "' , '" + field[3] + "' , '"+ field[4] + "' , '" + field[5] + "' );";
            SQL_cmd.CommandText = commandString;
            SQL_cmd.ExecuteNonQuery();
        }
    }
}
