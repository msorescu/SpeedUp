   M S B A M L   `   `   `  ����   SpeedUp    SpeedUp.WorkNow   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     -     � Title$  WorkNow��$	��623��$
��1016�� � WindowStartupLocation$ CenterScreen=� � DataContext ��  �� RelativeSource �� *Self+.��� $	��985��$	��568�� � RowDefinitions �� $
��556* ��� $	��12* �.+��� -   $��PerformanceLogProduction��$(��"Open Performance Log on Production��$��23��$��Right=�$��	0,435,0,0q�$	��Top=�$	��219���� -   $��PerformanceLogQa1��$!��Open Performance Log on Qa1��$��23��$��Right=�$��0,435,225,0q�$	��Top=�$	��206���� -   $��ModuleIDLabel��$��
Module ID:��$��27��$
��Left=�$��	12,17,0,0q�$	��Top=�$��77���� -   $��ModuleNameLabel��$��Module Name:��$��27��$
��Left=�$��
183,13,0,0q�$	��Top=�$��87���� -   $��BoardIDLabel��$��	Board ID:��$��25��$
��Left=�$��	12,48,0,0q�$	��Top=�$��75���� -   $��RDCCodeLabel��$��	RDC Code:��$��25��$
��Left=�$��
12,103,0,0q�$	��Top=�$��75���� -   $��TraceNameLabel��$��Trace Name:��$
��Left=�$��12,76,0,442q�$��77���� -	   $��OpenTFS��$��
Open In VS��$��24��$
��Left=�$��
870,50,0,0q�$	��Top=�$	��102���� -
   $��ModuleNameTextBox��$��23��$��
273,19,0,0q�$	��Top=�$
��Left=�$	��255���� -   $��TraceNameTextBox��$��23��$
��Left=�$��	86,78,0,0q�$	��Top=�$��84���� -   $��ModuleIDTextBox��$��23��$
��Left=�$��	86,17,0,0q�$	��Top=�$��84���� -   $��RDCCodeTextBox��$��23��$
��Left=�$��
86,107,0,0q�$	��Top=�$��84���� -   $��BoardIDTextBox��$��23��$
��Left=�$��	86,48,0,0q�$	��Top=�$��84���� -   $��LoginNameLabel��$��Login Name:��$��27��$
��Left=�$��
182,46,0,0q�$	��Top=�$	��108���� -   $��LoginNameTextBox��$��23��$��Right=�$��
0,50,623,0q�$	��Top=�$��89���� -   $��PasswordLabel��$��	Password:��$��23��$
��Left=�$��
363,48,0,0q�$	��Top=�$��96���� -   $��PasswordTextBox��$��23��$
��Left=�$��
444,48,0,0q�$	��Top=�$��84���� -   $��LoginURLLabel��$��
Login URL:��$��32��$
��Left=�$��185,105,0,0q�$	��Top=�$��85���� -   $��LoginURLTextBox��$��23��$
��Left=�$��273,107,0,0q�$	��Top=�$	��255���� -   $��UserAgentLabel��$��User Agent:��$��27��$
��Left=�$��
185,74,0,0q�$	��Top=�$��86���� -   $��UserAgentTextBox��$��23��$
��Left=�$��
273,78,0,0q�$	��Top=�$��89���� -   $��UAPasswordLabel��$��UA Password:��$��25��$
��Left=�$��
363,74,0,0q�$	��Top=�$��96���� -   $��UAPasswordTextBox��$��23��$
��Left=�$��
444,76,0,0q�$	��Top=�$��84���� -   $��	listView1��$	��232��$
��Left=�$��
12,146,0,0q�$	��Top=�$	��516������ *DefFileDataCollection+ �� View �� .P���  �� Width$	 100��$��	File Name�� �� DisplayMemberBinding �� *
FileName+�� $ 60��$��	File Size�� �� *
FileSize+�� $ 80��$��
Board Type�� �� *	BoardType+�� $ 80��$��Property Class�� �� *PropertyClass+�� $ 50��$��Version�� �� *	Version+�� $ 80��$��Checkout By�� �� *
CheckoutBy+�� $ 50��$��	Is Latest�� �� *
IsLatest+�� -   $��GetMetadataButton��$��Get MetaData��$��24��$
��Left=�$��553,146,0,0q�$	��Top=�$��96���� -   $��WorkingFolderTextBox��$��24��$��Right=�$��
0,13,122,0q�$	��Top=�$	��210���� -   $��OpenFolderButton��$��Open Folder��$��24��$��
870,18,0,0q�$	��Top=�$
��Left=�$	��102���� -   $��UploadDefFileQa1Button��$(��"Upload Selected Def File(s) to Qa1��$��24��$
��Left=�$��554,346,0,0q�$	��Top=�$	��206���� -   $"��GenerateDataAggRequestButton��$'��!Generate DataAgg Request 200 recs��$��24��$
��Left=�$��
14,384,0,0q�$	��Top=�$	��207���� -   $��GetSampleDataButton��$��Get Sample Data��$��24��$
��Left=�$��553,177,0,0q�$	��Top=�$��96���� -    $��OpenMetaDataButton��$��Open MetaData��$��24��$
��Left=�$��655,146,0,0q�$	��Top=�$��96���� -!   $��OpenSampleDataButton��$��Open Sam Data��$��24��$
��Left=�$��655,177,0,0q�$	��Top=�$��96���� -"   $��OpenMetaDataLogButton��$��Open Meta Log��$��24��$
��Left=�$��757,146,0,0q�$	��Top=�$��96���� -#   $��OpenSampleDataLogButton��$��Open Sam Log��$��24��$
��Left=�$��757,177,0,0q�$	��Top=�$��96���� -$   $��ReplyEmailButton��$��Reply Email��$��24��$
��Left=�$��713,496,0,0q�$	��Top=�$	��207��$y�Hidden=��� -%   $��MlsNumberTextBox��$��23��$
��Left=�$��655,207,0,0q�$	��Top=�$	��198���� -&   $��label1��$��MLS Number:��$��24��$
��Left=�$��553,205,0,0q�$	��Top=�$��96���� -'   $��AddCommentButton��$"��Add Comment To Selected Defs��$��24��$
��Left=�$��554,286,0,0q�$	��Top=�$	��206���� -(   $��IncreaseVersionButton��$*��$Increase Version Number for All Defs��$��24��$��Right=�$��	0,286,0,0q�$	��Top=�$	��219���� -)   $��CommentTextBox��$��45��$
��Left=�$��553,236,0,0q� � ToolTip$ Comments��$	��Top=�$	��300��	 �� TextWrapping$
	 Wrap=��� -*   $��DownloadFileTextBox��$��24��$
��Left=�$��
14,496,0,0q�$	��Top=�$	��261���� -+   $��DownLoadFileButton��$%��Download File to C:\TCSDownload��$��24��$
��Left=�$��281,495,0,0q�$	��Top=�$	��247���� -,   $��ResetDefQa1Button��$ ��Reset Defs from CSS on Qa1��$
��Left=�$��24��$��554,316,0,216q�$	��206���� --   $��ResetDefProductionButton��$'��!Reset Defs from CSS on Production��$��24��$��Right=�$��	0,316,0,0q�$	��Top=�$	��219���� -.   $ ��OpenStandardFieldDocButton��$��Open Standard Field Xls��$��24��$
��Left=�$��
13,532,0,0q�$	��Top=�$	��144���� -/   $&�� PublishCategorizationToQa1Button��$#��Publish Categorization to Qa1��$��24��$
��Left=�$��554,405,0,127q�$	��206���� -0   $-��'PublishCategorizationToProductionButton��$*��$Publish Categorization to Production��$��24��$
��Left=�$��766,405,0,127q�$	��219���� -1   $��label2��$��TFS Local Folder:��$��24��$
��Left=�$��
553,15,0,0q�$	��Top=�$	��103���� -2   $��TFSLocationTextBox��$��23��$��Right=�$��
0,47,121,0q�$	��Top=�$	��211���� -3   $��TFSLocationLabel��$��TFS Source Path:��$��24��$
��Left=�$��
553,45,0,0q�$	��Top=�$	��103���� -4   $��OpenTCSResultButton��$��Open TCS Result��$��24��$
��Left=�$��859,177,0,0q�$	��Top=�$	��113���� -5   $��SettingButton��$��Setting��$��24��$��Right=�$��0,495,324,0q�$	��Top=�$��95���� -6   $)��#RetrieveCategorizationFromQa1Button��$&�� Retrieve Categorization from Qa1��$��24��$
��Left=�$��554,376,0,156q�$	��206���� -7   $0��*RetrieveCategorizationFromProductionButton��$-��'Retrieve Categorization from Production��$��24��$��Right=�$��0,376,0,156q�$	��219���� -8   $%��GenerateDataAggRequestAllButton��$'��!Generate DataAgg Request All recs��$��24��$��Right=�$��0,384,552,0q�$	��Top=�$	��206���� -9   $ ��LaunchRETSClientToolButton��$��Launch RETS Client Tool��$��24��$
��Left=�$��
14,443,0,0q�$	��Top=�$	��208���� -:   $��LaunchHandyMapperButton��$��Launch Handy Mapper��$��24��$
��Left=�$��
14,413,0,0q�$	��Top=�$	��208���� -;   $��LaunchSearchToolButton��$��Launch Search Mapper��$��24��$
��Left=�$��227,413,0,0q�$	��Top=�$	��206���� -<   $��LaunchTesetHarnessButton��$��Launch Test Harness��$��24��$
��Left=�$��227,443,0,0q�$	��Top=�$	��206���� -=   $$��OpenOrcaDataSourceActiveButton��$&�� Open Orca Data Source Active Xls��$��24��$
��Left=�$��163,532,0,0q�$	��Top=�$	��193���� ->   $��label3��$?��9Download data file from Production, Staging or Qa1 server��$��28��$
��Left=�$��
13,473,0,0q�$	��Top=�$	��353���� -?   $��OpenTCSURLLinkPageButton��$��Open TCS URL Link Page��$��24��$��Right=�$��0,532,451,0q�$	��Top=�$	��166���� -@   $��ReleaseDEFFilesButton��$)��#Release DEF files to CSS Production��$��24��$
��Left=�$��766,346,0,186q�$	��219���� -A   $��CollectTimeZoneInfoButton��$��Collect TimeZone Info��$��24��$
��Left=�$��554,525,0,0q�$	��Top=�$	��141��$y�Hidden=��� -B   $��CheckOutButton��$��	Check Out��$��24��$��Right=�$��
0,78,122,0q�$	��Top=�$	��105���� -C   $��CheckInButton��$��Check In��$��24��$
��Left=�$��870,107,0,0q�$	��Top=�$	��102���� -D   $��GetLatestVersionButton��$��Get Latest Verion��$��24��$
��Left=�$��
653,78,0,0q�$	��Top=�$	��100���� -E   $��UndoCheckoutButton��$��Undo Checkout��$��24��$
��Left=�$��
870,78,0,0q�$	��Top=�$	��102���� -F   $��CheckInCommentsTextBox��$��24��$��Right=�$��0,109,122,0q�$	��Top=�$	��210���� -G   $��label4��$��	Comments:��$��24��$
��Left=�$��553,107,0,0q�$	��Top=�$��96���� -H   $��RunRequestButton��$��Run Search Request��$��24��$
��Left=�$��859,257,0,0q�$	��Top=�$	��126���� -I   $��GetAllSampleDataButton��$��Get All Sample Data��$��24��$
��Left=�$��859,146,0,0q�$	��Top=�$	��114���� -J   $��IncludeRETSBlobCheckBox��$��Include RETS Blob��$��16��$
��Left=�$��860,212,0,0q�$	��Top=��� -K   $��
TestButton��$
��Test��$��24��$��Right=�$��0,496,180,0q�$	��Top=�$��95��$y�Hidden=�