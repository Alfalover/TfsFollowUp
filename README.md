# Azure DevOps

## Introduction
  I Initially create it to Learning F# , but become an useful daily tool in my job to monitor Sprint health.
  
  Currently uses .NET Core 3, F# and a horrible javascript Bootstrap/JQuery and Chart.js 
  
  Current features: 
  
    - General summary screen 
		Counts Remaining work by task type (feature or bug)
		Counts completed work by task and compare with the remaining time 
		Taking into account Team members capacity 
		Summary of workitems by state
		Also check completeness of the hour informed by team members.
		
	- Tracking hour follow up 
		By Team member, compute how may hour are missing from its capacity 
		Allow show summary table where hour are set
		
    - WorkItem Followup
	    If Effort and estimation are set in hours, this page will be useful 
		it shows progress and deviation
		
	- Detailed Burndown 
	    Shows a burndown with all task stacked and allow play removing or adding
		Also you can open the burndown of a simple workitem.
	
	- Developer Burndown
	   Totally experimental
	   
	**This is completely experimental, no warranty**
	   
## Setup 

 This app uses TFS/Azure DevOps API to gather information, and hold it, on files at disk (yes I know, bad)
 
 Setup appsettings.json
  
 Currently only tested on Windows Domain 
 `<
 {
  "credentials": {
    "user": "user",
    "password": null,
    "domain": "domain"
  }
  >`
  
   `<
  ,
  "tfs": {
    "Url": "http://tfs.yourTFSurl.com",
    "ProjectName": "Project Name",
    "TeamName": "/Team Name",
    "WorkItemId": "User Story", 
    "BugId" : "Bug",
    "QueryId": "6b342a42-0be8-49fb-aa25-3db08bbb9903",
    "UpdatePeriodSeconds": 300,
    "UpdateDisabled": "true"
  },
   >`
   
     This application uses ProjectName and TeamName to request capacity and sprint list 
     the usual Azure DevOps api request is :
	 Url+ProjectName+TeamName+/Api blabla 
	 
	 You need to create a query in TFS where this application will gather workitems information 
	 usually when you are showing a query at Azure DevOps / TFS
	 http://url/DefaultCollection/ProjectName/_queries/query/c01d8865-1422-48a3-87d6-6ab4ed345e8c/
	 
	 UpdateDisabled should be False, I added that because I usually develop this app on my conmute to work
	 and I have a very unstable Internet communication. with True force app to no refresh from TFS and use
	 cache files.
  
  
  "folders": {
    "cache": "./Data"
  }
	where to store cache files.

}
 
 ### Running.
 Basically run TeamFollowUpConsole or its built executable.
 
## An Image worth more than thousand words

 Some Screenshots 
 
 ![screenshot](/screenshots/summary.png)
 ![screenshot](/screenshots/members.png)
 ![screenshot](/screenshots/memberdetail.png)
 ![screenshot](/screenshots/workitems.png)
 ![screenshot](/screenshots/burndown.png)
 ![screenshot](/screenshots/PR.png)
 
 