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
        private static String field_units = "Units";
        private static String field_distance = "Distance";
        private static String field_speed = "Speed";
        private static String field_Breaks = "Breaks_mins";
        private static String field_expectedInterval = "Totaltime";
        private static String field_begin = "StartCPFK";
        private static String field_end = "EndCPFK";

        // fields in the score table
        private static String field_competitor = "CompetitorFK";
        private static String field_stage = "StageFK";
        private static String field_timetaken = "timetaken";
        private static String field_score = "points";


        OpenFileDialog OFileDialog;
        SQLiteConnection dataBase;
        SQLiteDataAdapter rallyAdapter, compAdapter, checkptAdapter,
                          stageAdapter, timingAdapter, foundAdapter, scoreAdapter;
        String errorlog;
        DataSet ds_competitors, ds_checkpoints, ds_timings, ds_stages, ds_rally;
        DataTable dt_stages, dt_scores;
        // default values for penalties
        int time_allowed = 20;
        int miss_penalty = 50;
        int out_of_time_penalty = 25;

        public MainWindow()
        {
            String connectionString;
            // SQLiteCommand SQL_cmd;
            InitializeComponent();

            // version number in connection string  is the SQLite version and needs to be set to 3.
            connectionString = "Data Source =c:\\Databases\\Rally2019a.db;Version=3;New=True;Compress=True;";
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
            // FilelistBox.Items.Clear();
            SearchlistBox.Items.Clear();
            TextReader reader = scr.OpenText();
            string line = reader.ReadLine();
            if (line != null)
            {
                // dataBase.Open();

                while (line != null)
                {
                    // FilelistBox.Items.Add(line);
                    SearchlistBox.Items.Add(line);
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
            dropAllTables();
            createAllTables();
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
                field_units + " TEXT, " +  field_distance + " FLOAT, " + 
                field_speed + " INTEGER, " + field_Breaks + " INTEGER, " + 
                field_expectedInterval + " FLOAT, " +  field_begin + " INTEGER, " + field_end + " INTEGER);";
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

        private void ResetScores_Click(object sender, RoutedEventArgs e)
        {
            SQLiteCommand SQL_cmd;
            SQL_cmd = dataBase.CreateCommand();
            // drop score table
            SQL_cmd.CommandText = "DROP TABLE " + table_scores;
            SQL_cmd.ExecuteNonQuery();

            SQL_cmd.CommandText = "CREATE TABLE IF NOT EXISTS " + table_scores +
               " (scID integer primary key, " + field_competitor + " INTEGER, " +
                field_stage + " INTEGER, " + field_timetaken + " FLOAT, " +
                field_score + " INTEGER);";
            SQL_cmd.ExecuteNonQuery();
        }

        private void Show_Click(object sender, RoutedEventArgs e)
        {
            // This shows scores in OutputDocument
            
            String outputString;
            DataRow[] scores_rows = dt_scores.Select();
            DataRow[] compets = ds_competitors.Tables[0].Select();
            DataRow[] foundrows;
            DataRow score_row,comp_row, found_row;
            int CompFK, StageFK;
            Bold boldtext = new Bold();
            outPutDocument.Blocks.Clear();
            // ds_rally dataset contains data on the rally 
            outputString = ds_rally.Tables[0].Rows[0][1].ToString() + "  "+ ds_rally.Tables[0].Rows[0][2].ToString();
            boldtext.Inlines.Add(outputString +'\n');
            Paragraph para = new Paragraph();
            para.FontSize = 20;
           
            para.Inlines.Add(boldtext);
            outputString = "All Scores";
            boldtext.Inlines.Add(outputString +'\n'+'\r');
            para.Inlines.Add(boldtext);
            
            // add heading paragraph to document 
            outPutDocument.Blocks.Add(para);


           //  para = new Paragraph();
           //  para.FontSize = 12;
            // dataTable dt_scores contains the scores These are in rows
            // ned to do following for each row 
            score_row = scores_rows[0];
            CompFK = Int32.Parse(score_row[1].ToString());
            StageFK = Int32.Parse(score_row[2].ToString());
            outputString = "Comp# " + CompFK+ " Stage# " + StageFK +"  " + score_row[3].ToString() +
                "  " + score_row[4].ToString() + "  ";
            // para.Inlines.Add(outputString + '\n' + '\r');

            // outPutDocument.Blocks.Add(para);


            // or consider starting from each entry in ds_competitors
            para = new Paragraph();
            para.FontSize = 14;
            
            comp_row = compets[0];
            outputString = "Comp # "+ comp_row[1].ToString() + "  " + comp_row[2].ToString() + "  " + comp_row[3].ToString() +
                "  " + comp_row[4].ToString() + "  ";
            boldtext.Inlines.Add(outputString + '\n' + '\r');
            para.Inlines.Add(boldtext);
            outPutDocument.Blocks.Add(para);
            // get the index of this entry
            String compindex = comp_row[0].ToString();
            // para.Inlines.Add("index of entry " + compindex + '\n' + '\r');
            // outPutDocument.Blocks.Add(para);
            // look for comindex in scores
            //below code from  the search button code
            String comString;
            SQLiteCommand sqlCmd;
            // String findString = searcefor.Text;
            
            // build command and string
            sqlCmd = dataBase.CreateCommand();
            comString = "SELECT * FROM " + table_timing + " WHERE " + field_compFK + " = " + compindex + ";";
            sqlCmd.CommandText = comString;

            // execute the sqlcommand to return a datagrid
            foundAdapter = new SQLiteDataAdapter(sqlCmd);
            DataSet foundDataset = new DataSet();
            foundAdapter.Fill(foundDataset, "Found Table");
            foundDataGrid.ItemsSource = foundDataset.CreateDataReader();
            // this should populate data found into the founddatagrid
            foundrows = foundDataset.Tables[0].Select();
            // outputString = "No of records found = " + foundrows.Length + '\n';
            // para.Inlines.Add(outputString + '\n' + '\r');
            outPutDocument.Blocks.Add(para);
            // this gives us the actiual timing event
            foreach (DataRow r in foundrows)
            {
                outputString = r[0].ToString() + "  " + r[1].ToString() + "  " + r[2].ToString() + "  " +
                    r[3].ToString() + "  " + r[4].ToString() + '\n';
                para.Inlines.Add(outputString + '\r');
                outPutDocument.Blocks.Add(para);
            }
            // we also need to do same for scores
            para = new Paragraph();
            sqlCmd = dataBase.CreateCommand();
            comString = "SELECT * FROM " + table_scores + " WHERE " + field_competitor + " = " + compindex + ";";
            sqlCmd.CommandText = comString;
            // execute the sqlcommand to return a datagrid
            foundAdapter = new SQLiteDataAdapter(sqlCmd);
            foundDataset = new DataSet();
            foundAdapter.Fill(foundDataset, "Found Table");
            // this might cause problems without clearing data from founddatagrid
            foundDataGrid.ItemsSource = foundDataset.CreateDataReader();
            // this should populate data found into the founddatagrid

            foundrows = foundDataset.Tables[0].Select();
            outputString = "No of records found = " + foundrows.Length + '\n';
            para.Inlines.Add(outputString + '\n' + '\r');
            outPutDocument.Blocks.Add(para);
            foreach (DataRow r in foundrows)
            {
                outputString = r[0].ToString() + "  " + r[1].ToString() + "  " + r[2].ToString() + "  " +
                    r[3].ToString() + "  "  + '\n';
                para.Inlines.Add(outputString + '\r');
                outPutDocument.Blocks.Add(para);
            }
        }

        private void Print_btn_Click(object sender, RoutedEventArgs e)
        {
            // prints the contnet of the FlowDocument control.
            docReader.Print();
        }

        private void Show_Times_Click(object sender, RoutedEventArgs e)
        {
            String outputString;
            DataRow[] scores_rows = dt_scores.Select();
            DataRow[] compets = ds_competitors.Tables[0].Select();
            DataRow[] foundrows;
            DataRow score_row,  found_row;
            int CompFK, StageFK;

            Bold boldtext = new Bold();
            outPutDocument.Blocks.Clear();
            Paragraph para = new Paragraph();
            para.FontSize = 20;
            // ds_rally dataset contains data on the rally 
            outputString = ds_rally.Tables[0].Rows[0][1].ToString() + "  " + ds_rally.Tables[0].Rows[0][2].ToString();

            boldtext.Inlines.Add(outputString + '\n');
            para.Inlines.Add(boldtext);
            outputString = "All Times";
            boldtext.Inlines.Add(outputString + '\n' + '\r');
            para.Inlines.Add(boldtext);
            // add heading paragraph to document 
            outPutDocument.Blocks.Add(para);
            
            

            foreach(DataRow comp_row in compets)
            {
                para = new Paragraph();
                para.FontSize = 14;
                outputString = "Comp # " + comp_row[1].ToString() + "  " + comp_row[2].ToString() + "  " + comp_row[3].ToString() +
                "  " + comp_row[4].ToString() + "  ";
            boldtext.Inlines.Add(outputString + '\n' + '\r');
            para.Inlines.Add(boldtext);
            // outPutDocument.Blocks.Add(para);
            // get the index of this entry
            String compindex = comp_row[0].ToString();
            
            String comString;
            SQLiteCommand sqlCmd;
            // String findString = searcefor.Text;

            // build command and string
            sqlCmd = dataBase.CreateCommand();
            comString = "SELECT * FROM " + table_timing + " WHERE " + field_compFK + " = " + compindex + ";";
            sqlCmd.CommandText = comString;

            // execute the sqlcommand to return a datagrid
            foundAdapter = new SQLiteDataAdapter(sqlCmd);
            DataSet foundDataset = new DataSet();
            foundAdapter.Fill(foundDataset, "Found Table");
            foundDataGrid.ItemsSource = foundDataset.CreateDataReader();
            // this should populate data found into the founddatagrid
            foundrows = foundDataset.Tables[0].Select();
            // outputString = "No of records found = " + foundrows.Length + '\n';
            // para.Inlines.Add(outputString + '\n' + '\r');
            // outPutDocument.Blocks.Add(para);
            // this gives us the actiual timing event
            foreach (DataRow r in foundrows)
            {
                outputString = "Checkpoint # " + r[2].ToString() + " timed @ " + r[3].ToString() + " by " + r[4].ToString()  + '\n';
                para.Inlines.Add(outputString + '\r');
                // outPutDocument.Blocks.Add(para);
            }
                outPutDocument.Blocks.Add(para);
            }
        }

        private void Show_Scores_Click(object sender, RoutedEventArgs e)
        {
            String outputString;
            DataRow[] scores_rows = dt_scores.Select();
            DataRow[] compets = ds_competitors.Tables[0].Select();
            DataRow[] foundrows;
            DataRow score_row,  found_row;
            int CompFK, StageFK;
            String comString, compindex;
            SQLiteCommand sqlCmd;

            Bold boldtext = new Bold();
            outPutDocument.Blocks.Clear();
            Paragraph para = new Paragraph();
            para.FontSize = 20;
            // ds_rally dataset contains data on the rally 
            outputString = ds_rally.Tables[0].Rows[0][1].ToString() + "  " + ds_rally.Tables[0].Rows[0][2].ToString();
            boldtext.Inlines.Add(outputString + '\n');
           para.Inlines.Add(boldtext);
            outputString = "All Scores";
            boldtext.Inlines.Add(outputString + '\n' + '\r');
            para.Inlines.Add(boldtext);
            // add heading paragraph to document 
            outPutDocument.Blocks.Add(para);

            para = new Paragraph();
            para.FontSize = 14;
            foreach (DataRow comp_row in compets)
            {
                // write the competition nunber, name etc of this competitor.
                outputString = "Comp# " + comp_row[1].ToString() + "  Name " + comp_row[2].ToString() +
                    "  Machine " + comp_row[3].ToString() + "  Machine " + comp_row[4].ToString();
                boldtext.Inlines.Add(outputString + '\n' + '\r');
                compindex = comp_row[0].ToString();
                
                // now get scores
                sqlCmd = dataBase.CreateCommand();
                comString = "SELECT * FROM " + table_scores + " WHERE " + field_competitor + " = " + compindex + ";";
                sqlCmd.CommandText = comString;

                // execute the sqlcommand to return a datagrid
                foundAdapter = new SQLiteDataAdapter(sqlCmd);
                DataSet foundDataset = new DataSet();
                foundAdapter.Fill(foundDataset, "Found Table");
                foundDataGrid.ItemsSource = foundDataset.CreateDataReader();
                // this should populate data found into the founddatagrid
                foundrows = foundDataset.Tables[0].Select();
                
                foreach (DataRow r in foundrows)
                {
                    outputString = "Stage# " + r[2].ToString() + "; time taken " + r[3].ToString() + " mins. Penalty Points=  " + r[4].ToString() + '\n';
                    para.Inlines.Add(outputString + '\r');
                    // outPutDocument.Blocks.Add(para);
                }
                outPutDocument.Blocks.Add(para);

            }

        }

        private void Show_Results_Click(object sender, RoutedEventArgs e)
        {
            // this is used to order the competitors by score 
            // not too sure how to do this
            DataRow[] joinedData;
            String comString;
            SQLiteCommand sqlCmd;
            String findString = searcefor.Text;
            Bold boldtext;
            String EventName, EventDate; ;

            outPutDocument.Blocks.Clear();

            EventName = ds_rally.Tables[0].Rows[0][1].ToString();
            EventDate = ds_rally.Tables[0].Rows[0][2].ToString();
            Paragraph para = new Paragraph();
            para.FontSize = 20;
            para.Foreground = Brushes.Blue;
            para.FontWeight = FontWeights.Bold;
            para.Inlines.Add('\t'+ EventName +  '\n');
            // outPutDocument.Blocks.Add(para);
                      
            // para.Inlines.Add(" "+'\t' + '\t' + " Held on " + EventDate + '\n' + '\n');
            para.Inlines.Add(" " + '\t' + '\t' + "  Competition Results " + '\n');
            outPutDocument.Blocks.Add(para);

            // create a Result table 
            // create integers to keep track of row nuners and rider position
            int rownumner = 0;
            int currentscore = -1;
            int finishposition = 0;
            int rider = 1;

            Table resulttable = new Table();
            resulttable.Margin = new Thickness(60, 0, 0, 0);
            outPutDocument.Blocks.Add(resulttable);

            // add 5 columns
            resulttable.Columns.Add(new TableColumn());
            resulttable.Columns[0].Width = new GridLength(50);
            resulttable.Columns.Add(new TableColumn());
            resulttable.Columns[1].Width = new GridLength(70);
            resulttable.Columns.Add(new TableColumn());
            resulttable.Columns[2].Width = new GridLength(100);
            resulttable.Columns.Add(new TableColumn());
            resulttable.Columns[3].Width = new GridLength(250);
            resulttable.Columns.Add(new TableColumn());
            resulttable.Columns[4].Width = new GridLength(100);

            // add title row
            TableRow currentrow;
            resulttable.RowGroups.Add(new TableRowGroup());
            resulttable.RowGroups[0].Rows.Add(new TableRow());
            currentrow = resulttable.RowGroups[0].Rows[rownumner];
            currentrow.Background = Brushes.WhiteSmoke;
            currentrow.FontSize = 30;
            currentrow.FontWeight = FontWeights.Bold;
            currentrow.Cells.Add(new TableCell(new Paragraph(new Run("RESULTS"))));
            currentrow.Cells[0].ColumnSpan = 5;
            currentrow.Cells[0].TextAlignment = TextAlignment.Center;

            // add header row
            rownumner++;
            resulttable.RowGroups[0].Rows.Add(new TableRow());
            currentrow = resulttable.RowGroups[0].Rows[rownumner];
            currentrow.FontSize = 18;
            currentrow.FontWeight = FontWeights.Bold;
            currentrow.Background = Brushes.WhiteSmoke;
            currentrow.Cells.Add(new TableCell(new Paragraph(new Run("POSITION"))));
            currentrow.Cells[0].ColumnSpan = 2;
            currentrow.Cells[0].TextAlignment = TextAlignment.Center;
            currentrow.Cells.Add(new TableCell(new Paragraph(new Run("CompNo."))));
            currentrow.Cells.Add(new TableCell(new Paragraph(new Run("NAME"))));
            currentrow.Cells.Add(new TableCell(new Paragraph(new Run("SCORE"))));

            // add blank row
            rownumner++;
            resulttable.RowGroups[0].Rows.Add(new TableRow());
            currentrow = resulttable.RowGroups[0].Rows[rownumner];
            currentrow.FontSize = 18;
            currentrow.FontWeight = FontWeights.Bold;
            currentrow.Background = Brushes.WhiteSmoke;
            currentrow.Cells.Add(new TableCell(new Paragraph(new Run("   "))));
            currentrow.Cells[0].ColumnSpan = 4;
            currentrow.Cells[0].TextAlignment = TextAlignment.Center;



            sqlCmd = dataBase.CreateCommand();

            comString = "SELECT compID, compNumber, Competitor_Name, sum(points) AS totalPoints FROM competitors JOIN scores ON competitorFK = compID GROUP BY compNumber ORDER BY totalPoints;";
            sqlCmd.CommandText = comString;
            
            
                       
           
            
           

            foundAdapter = new SQLiteDataAdapter(sqlCmd);
            DataSet foundDataset = new DataSet();
            foundAdapter.Fill(foundDataset, "My Table");
            foundDataGrid.ItemsSource = foundDataset.CreateDataReader();

            joinedData = foundDataset.Tables[0].Select();
            int position = 0;
            int total = 0;
            string jtext = "unknown";
            
            foreach (DataRow r in joinedData)
            {
                                
                rownumner++;
                resulttable.RowGroups[0].Rows.Add(new TableRow());
                currentrow = resulttable.RowGroups[0].Rows[rownumner];
                currentrow.FontSize = 14;

                // check for joint position
                if (Int32.Parse(r[3].ToString()) > currentscore)
                    {
                       jtext = "";
                       position = rider;
                       currentscore = Int32.Parse(r[3].ToString());
                     } else
                    {
                    jtext = "joint";
                    }

                // add jtext to first cell
                currentrow.Cells.Add(new TableCell(new Paragraph(new Run(jtext))));

                // check for first three positions
                if (position < 4)
                {
                    currentrow.FontWeight = FontWeights.Bold;
                    currentrow.Foreground = Brushes.RoyalBlue;
                }
                // and add position number
                switch (position)
                {
                    case 1: currentrow.Cells.Add(new TableCell(new Paragraph(new Run("WINNER"))));
                        break;
                    case 2: currentrow.Cells.Add(new TableCell(new Paragraph(new Run("SECOND" ))));
                        break;
                    case 3: currentrow.Cells.Add(new TableCell(new Paragraph(new Run("THIRD" ))));
                        break;
                    default: currentrow.Cells.Add(new TableCell(new Paragraph(new Run(position + "th"))));
                        break;
                }

               

                // add competition number
                currentrow.Cells.Add(new TableCell(new Paragraph(new Run("  " + r[1].ToString() ))));
                // add name
                currentrow.Cells.Add(new TableCell(new Paragraph(new Run("  " + r[2].ToString()))));
                // add score
                currentrow.Cells.Add(new TableCell(new Paragraph(new Run("  " + r[3].ToString()))));

                rider++;
            }

        }

       
        private void JoinTables_Click(object sender, RoutedEventArgs e)
        {
            // This uses a select on joined competitor and score tables to return exaclt what is needed for result
            DataRow[] joinedData;
            String comString;
            SQLiteCommand sqlCmd;
            String findString = searcefor.Text;
            Bold boldtext;
            // assume database is open

            // build command and string
            sqlCmd = dataBase.CreateCommand();
            // comString = "SELECT * FROM " + table_scores + " JOIN " + table_timing + ";";
            // comString = "SELECT * FROM " + table_scores + " JOIN " + table_timing + " WHERE " + table_scores + "."+ 
            //     field_competitor + " = " + findString + ";";
            comString = "SELECT " + field_compNo + ", " + field_CompName + ", " + field_stage + ", " + field_timetaken + ", "
                +  field_score + " FROM " + table_competitor + " JOIN " + table_scores
                + " ON " +  field_competitor + " = compID" 
                + " ORDER BY compID "  + ";";

            outPutDocument.Blocks.Clear();
            Paragraph para = new Paragraph();
            para.Inlines.Add(comString + '\n');
            outPutDocument.Blocks.Add(para);

            sqlCmd.CommandText = comString;
            // execute the sqlcommand to return a datagrid
            foundAdapter = new SQLiteDataAdapter(sqlCmd);
            DataSet foundDataset = new DataSet();
            foundAdapter.Fill(foundDataset, "My Table");
            foundDataGrid.ItemsSource = foundDataset.CreateDataReader();
            // this should populate data found into the founddatagrid
            // Data correct in FoundDataGrid.
            // now need to print on OutputDocument
            joinedData = foundDataset.Tables[0].Select();
            int comp_number = 0;
            int total = 0;
            para = new Paragraph();
            foreach (DataRow r in joinedData)
            {
                if (Int32.Parse(r[0].ToString()) != comp_number)
                {
                    if (comp_number > 0)
                    {
                        boldtext = new Bold();
                        boldtext.Inlines.Add("Total points for competitor " + comp_number + " = " + total + '\n' + '\r');
                        para.Inlines.Add(boldtext);
                        outPutDocument.Blocks.Add(para);
                        
                    }
                    comp_number = Int32.Parse(r[0].ToString());
                    para.Inlines.Add("Comp No# " + r[0].ToString() + '\t' + " Stage " + r[2].ToString() +'\t' + " , Time taken " + r[3].ToString() + '\t' + " , Points " + r[4].ToString() + '\n');
                    outPutDocument.Blocks.Add(para);
                    total = Int32.Parse(r[4].ToString()); 
                }
                else
                {
                    total = total + Int32.Parse(r[4].ToString());
                    para.Inlines.Add("as above " + '\t' + ", Stage " + r[2].ToString() + '\t' + " , Time taken " + r[3].ToString() + '\t' + " , Points " + r[4].ToString() + '\n');
                    outPutDocument.Blocks.Add(para);
                }
                
            }
            // better to add this data to a table. to standardise format



        }

        private void editStageButton_Click(object sender, RoutedEventArgs e)
        {
            // This method links a stage to the start and end checkpoints for the stage
            double distance, expected;
            int speed, breaks;
            SQLiteCommand sqlCmd;
            String comString;
            SQLiteDataReader datareader;
            string stagename, foundrow;
            int stindex,  record;
            ArrayList   checkpoints;
            List<String> checkpointlist;
            // String cp;

            if (StagedataGrid.SelectedItem != null)
            // calls edit stage dialog if a stage is selected
            {
                stindex = StagedataGrid.SelectedIndex;
                DataRow[] rows = dt_stages.Select();
                if (rows != null)
                {
                    DataRow row = rows[stindex];

                    stagename = "stage: " + row[1];
                    distance = Double.Parse(row[3].ToString());
                    speed = Int32.Parse(row[4].ToString());
                    breaks = Int32.Parse(row[5].ToString());
                    expected = (distance * 60 / speed) + breaks;
                    // name is in column 1 of the stage row
                }
                else {
                    stagename = "nothing";
                    expected = 0;
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
                    // beginlabel.Content = CPs[0];
                    // finishlabel.Content = CPs[1];
                    DataRow row = rows[stindex];
                    row[7] = 1;
                    row[8] = 2;
                    SearchlistBox.Items.Add("stage " + row[1] + row[7]+ row[8]);
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
                    row[7] = int.Parse(foundrow);

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
                    row[8] = int.Parse(foundrow);
                    // calculate expected time
                    

                    // now update table_stage
                    sqlCmd = dataBase.CreateCommand();
                    comString = "UPDATE stages SET StartCPFK = '"+row[7]+ "' , EndCPFK = '"+ row[8] + 
                        "' , Totaltime = '" + expected + "' WHERE StageName  = '" + row[1] + "'";
                    sqlCmd.CommandText = comString;
                    record = sqlCmd.ExecuteNonQuery();
                    SearchlistBox.Items.Add("Updated stage records " + row[1]);
                }

            }

        }

        private void Search_both_Click(object sender, RoutedEventArgs e)
        {
            // called to search both timing and score tables
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

                comString = "SELECT competitorFK, sum(points) AS total FROM scores JOIN competitors ON  competitors.compID = scores.CompetitorFK GROUP BY competitorFK ORDER BY total, Competitor_Name;";
                // comString = "SELECT * FROM " + table_scores + " JOIN " + table_timing + " WHERE " + table_scores + "."+ 
                //     field_competitor + " = " + findString + ";";
               // comString = "SELECT " + table_scores + "." + field_competitor +", "+ field_stage +", "  + field_chckptFK +", "
               //     + field_time +", " + field_timetaken+ ", " +field_score + " FROM " + table_scores + " JOIN " + table_timing 
                //    + " WHERE " + table_scores + "." +  field_competitor + " = " + findString 
                //    + " ORDER BY " + table_scores + "." + field_competitor + ", " + field_stage + ";";
                outPutDocument.Blocks.Clear();
                Paragraph para = new Paragraph();
                para.Inlines.Add(comString +'\n');
                outPutDocument.Blocks.Add(para);

                sqlCmd.CommandText = comString;

                // execute the sqlcommand to return a datagrid
                foundAdapter = new SQLiteDataAdapter(sqlCmd);
                DataSet foundDataset = new DataSet();
                foundAdapter.Fill(foundDataset, "My Table");
                foundDataGrid.ItemsSource = foundDataset.CreateDataReader();
                // this should populate data found into the founddatagrid

            }
        }
    

        private void Search_Scores_Click(object sender, RoutedEventArgs e)
        {
            // called by Scores button to search for Scores on selected competitor
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
                comString = "SELECT * FROM " + table_scores + " WHERE " + field_competitor + " = " + findString + ";";
                sqlCmd.CommandText = comString;

                // execute the sqlcommand to return a datagrid
                foundAdapter = new SQLiteDataAdapter(sqlCmd);
                DataSet foundDataset = new DataSet();
                foundAdapter.Fill(foundDataset, "My Table");
                foundDataGrid.ItemsSource = foundDataset.CreateDataReader();
                // this should populate data found into the founddatagrid
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
        


        private String FindTiming(String Comp_Rec, String CP_Rec)
        {
            String comString, timing;
            SQLiteCommand sqlCmd;

            // create selection text
            sqlCmd = dataBase.CreateCommand();
            comString = "SELECT * FROM " + table_timing + " WHERE " + field_compFK + " = " + Comp_Rec + " AND " +
                  field_chckptFK +" = " + CP_Rec +";";
            sqlCmd.CommandText = comString;
            foundAdapter = new SQLiteDataAdapter(sqlCmd);
            
            DataTable foundTimings = new DataTable("Found");
            foundAdapter.Fill(foundTimings);
            if (foundTimings.Rows.Count == 0)
            {
                // no row returned
                timing = "missing";
            } else
            { foundDataGrid.ItemsSource = foundTimings.DefaultView;
                DataRow[] foundrows = foundTimings.Select();
                DataRow first = foundrows[0];
                // int r = Int32.Parse(first[0].ToString());
                timing = first[3].ToString();
                // this should populate data found into the founddatagrid 
            }
                return timing;

        }

        private List<String> get_checkpoints()
        {
            List<String> entries = new List<string>();
            SQLiteCommand sqlCmd = dataBase.CreateCommand();
            String comString = "SELECT * FROM " + table_checkpoint;
            sqlCmd.CommandText = comString;
            SQLiteDataReader query;
                       
            try
            {
                query = sqlCmd.ExecuteReader();

            }
            catch (SQLiteException error)
            {
               return entries;
               // should handle error
            }
            while (query.Read())
            {
                entries.Add(query.GetString(1));
            }
           
            return entries;
        }

        // called when the user clicks the Calculate button
        private void button_Click(object sender, RoutedEventArgs e)
        {
            // Check that all information required for the calculation is now present
            // stages have link to chckpoints
            String comString;
            SQLiteCommand sqlCmd;
            String findString = searcefor.Text;
            String st_string, end_string, dispString;
            DateTime starttime, endtime;
            TimeSpan timeTaken;            
            DataTable competitors = ds_competitors.Tables[0];
            DataRow[] rows = competitors.Select();
            DataRow[] stagedetails = dt_stages.Select();
            int No_of_stages = stagedetails.Length;
            DataRow thisStage;
            int startCP_record, endCP_record, stage_rec;
            int found, points;
            int speed;
            double distance, expected;

            DataRow row;
            int recordnumber, pointss;
            int competitornumber;
            int Nocompetitors = rows.Length;
            SearchlistBox.Items.Clear();

            // for each competitor
            for (int c = 0; c < Nocompetitors; c++)
            //for (int c = 0; c < 1; c++)
            {
                row = rows[c];
                // parse to integers
                //
                recordnumber = Int32.Parse(row[0].ToString());
                competitornumber = Int32.Parse(row[1].ToString());
                SearchlistBox.Items.Add("c ="+c+" "+recordnumber + " " + competitornumber + " " + row[2]);

                // for each stage
                for (int st = 0; st < No_of_stages; st++)
                {
                    thisStage = stagedetails[st];
                    // read values from thisStage
                    startCP_record = Int32.Parse(thisStage[7].ToString());
                    endCP_record = Int32.Parse(thisStage[8].ToString());
                    distance = Double.Parse(thisStage[3].ToString());
                    speed = Int32.Parse(thisStage[4].ToString());
                    stage_rec = Int32.Parse(thisStage[0].ToString());
                    expected = (distance * 60 / speed ) + Int32.Parse(thisStage[5].ToString());
                    SearchlistBox.Items.Add(thisStage[1] + " " + startCP_record + " " + endCP_record);

                    // need to search timing database for record which matches recordnumber and startCP_record
                    st_string = FindTiming(row[0].ToString(), thisStage[7].ToString());
                    SearchlistBox.Items.Add(recordnumber + " " + startCP_record + " " + st_string);
                    
                    
                    // need to search timing database for record which matches recordnumber and endCP_record
                    end_string = FindTiming(row[0].ToString(), thisStage[8].ToString());
                    SearchlistBox.Items.Add(recordnumber + " " + endCP_record + " " + end_string);

                    if (st_string.Equals("missing") || end_string.Equals("missing"))
                    {
                        // at least one timing is missing
                        SearchlistBox.Items.Add("Timing is missing  Start " + st_string + " - End "
                        + end_string + " Missing penalty " + "<> " + expected);
                        points = miss_penalty; // should be missing penalty
                        sqlCmd = dataBase.CreateCommand();
                        comString = "INSERT INTO " + table_scores + " (" +
                            field_competitor + ", " + field_stage + ", " + field_timetaken + ", " +
                            field_score + ") VALUES ('"
                            + recordnumber + "', '" + stage_rec + "' , '" + 0 + "' , '" +
                            points + "' );";
                        sqlCmd.CommandText = comString;
                        sqlCmd.ExecuteNonQuery();
                    }
                    else
                    {
                        starttime = DateTime.Parse(st_string);
                        endtime = DateTime.Parse(end_string);
                        timeTaken = endtime - starttime;
                        SearchlistBox.Items.Add("Datetime = " + starttime.ToLocalTime() + " - "
                            + endtime.ToLocalTime() + " = " + timeTaken.TotalMinutes + " <> " + expected);
                        points = (Int32)Math.Abs(timeTaken.TotalMinutes - expected);
                        if (points > time_allowed)
                        {
                            points = out_of_time_penalty;
                        }
                        sqlCmd = dataBase.CreateCommand();
                        comString = "INSERT INTO " + table_scores + " (" +
                            field_competitor + ", " + field_stage + ", " + field_timetaken + ", " +
                            field_score + ") VALUES ('"
                            + recordnumber + "', '" + stage_rec + "' , '" + timeTaken.TotalMinutes + "' , '" +
                            points + "' );";
                        sqlCmd.CommandText = comString;
                        sqlCmd.ExecuteNonQuery();
                    }
                    

                }
            }

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
               // called by the readbutton_click method ( button labeled "display"
                String comString;
                SQLiteCommand sqlCmd;
                           

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
                // SearchlistBox.Items.Add("time_allowed " + time_allowed);
                // SearchlistBox.Items.Add("out of time penalty " + out_of_time_penalty);
                // SearchlistBox.Items.Add("missed penalty " + miss_penalty);
                sqlCmd = dataBase.CreateCommand();
                comString = "SELECT * FROM " + table_rally;
                sqlCmd.CommandText = comString;
                rallyAdapter = new SQLiteDataAdapter(sqlCmd);
                ds_rally = new DataSet();
                rallyAdapter.Fill(ds_rally, "rallyTable");
                RallydataGrid.ItemsSource = ds_rally.CreateDataReader();
                time_allowed = Int32.Parse(ds_rally.Tables[0].Rows[0][6].ToString());
                out_of_time_penalty = Int32.Parse(ds_rally.Tables[0].Rows[0][7].ToString());
                miss_penalty = Int32.Parse(ds_rally.Tables[0].Rows[0][8].ToString());
                SearchlistBox.Items.Add("time_allowed " + time_allowed);
                SearchlistBox.Items.Add("out of time penalty " + out_of_time_penalty);
                SearchlistBox.Items.Add("missed penalty " + miss_penalty);


            // scores
                sqlCmd = dataBase.CreateCommand();
                comString = "SELECT * FROM " + table_scores;
                sqlCmd.CommandText = comString;
                scoreAdapter = new SQLiteDataAdapter(sqlCmd);
                dt_scores = new DataTable("Scores");
                scoreAdapter.Fill(dt_scores);
                scoresDataGrid.ItemsSource = dt_scores.DefaultView;



        }

        private void readButton_Click(object sender, RoutedEventArgs e)
        {
            // this is the method called when the button labelled"Display" is clicked
            loadfromDB();
        }

        private void OFileDialog_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Read setup file
            string fullpathname = OFileDialog.FileName;
            FileInfo scr = new FileInfo(fullpathname);
            //
            // FilelistBox.Items.Clear();
            SearchlistBox.Items.Clear();
            TextReader reader = scr.OpenText();
            // read first line with rally name etc
            string line = reader.ReadLine();
            if (line != null )
            {
                addrallydetails(line);
                // FilelistBox.Items.Add(line);
                SearchlistBox.Items.Add(line);

                // get next lines. These are stage details
                line = reader.ReadLine();
                while (line != null)
                {
                    //FilelistBox.Items.Add(line);
                    SearchlistBox.Items.Add(line);
                    addstagedetails(line);
                    line = reader.ReadLine();
                }

            }
          
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
