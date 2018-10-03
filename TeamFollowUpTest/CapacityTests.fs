module CapacityTests

open System
open Xunit
open Tfs

let defaultTeamMember = {id="1";displayName="user";uniqueName ="0";imageUrl=""}
let defaultMCR = {teamMember={defaultTeamMember with id="1"};
                  activities=[| {name="Development";capacityPerDay=7.0} |];
                  daysOff=[||]}

let defaultSprint = { id="0";name="Sprint";path="";url="";
                      attributes={startDate= Nullable (new DateTime(2018,9,13));
                                  finishDate= Nullable (new DateTime(2018,9,24));
                                  timeFrame=TimeFrame.current}}

[<Fact>]
let `` No DaysOff`` () =

    let m = {defaultMCR with daysOff=[|{start=new DateTime(2018,1,3);
                                        ``end``=new DateTime(2018,1,3)}|]}
    let t = List.empty<DaysOff>
    let sStart = Nullable (new DateTime(2018,9,13))
    let sEnd   = Nullable (new DateTime(2018,9,24))
    
    Assert.Equal(56.0,Capacity.GetUserCapacity(m,sStart,sEnd,t))

[<Fact>]
let ``last day teamDayOff`` () =

    let m = {defaultMCR with daysOff=[|{start=new DateTime(2018,1,3);
                                        ``end``=new DateTime(2018,1,3)}|]}
    let t = [{start=new DateTime(2018,9,24);``end``=DateTime(2018,9,24)}]
    let sStart = Nullable (new DateTime(2018,9,13))
    let sEnd   = Nullable (new DateTime(2018,9,24))
    
    Assert.Equal(49.0,Capacity.GetUserCapacity(m,sStart,sEnd,t))

[<Fact>]
let ``first day teamDayOff`` () =

    let m = {defaultMCR with daysOff=[|{start=new DateTime(2018,1,3);
                                        ``end``=new DateTime(2018,1,3)}|]}
    let t = [{start=new DateTime(2018,9,13);``end``=new DateTime(2018,9,13)}]
    let sStart = Nullable (new DateTime(2018,9,13))
    let sEnd   = Nullable (new DateTime(2018,9,24))
    
    Assert.Equal(49.0,Capacity.GetUserCapacity(m,sStart,sEnd,t))

[<Fact>]
let ``middle day teamDayOff`` () =

    let m = {defaultMCR with daysOff=[|{start=new DateTime(2018,1,3);
                                        ``end``=new DateTime(2018,1,3)}|]}
    let t = [{start=new DateTime(2018,9,21);``end``=new DateTime(2018,9,21)}]
    let sStart = Nullable (new DateTime(2018,9,13))
    let sEnd   = Nullable (new DateTime(2018,9,24))
    
    Assert.Equal(49.0,Capacity.GetUserCapacity(m,sStart,sEnd,t))

[<Fact>]
let ``range day teamDayOff`` () =

    let m = {defaultMCR with daysOff=[|{start=new DateTime(2018,1,3);
                                        ``end``=new DateTime(2018,1,3)}|]}
    let t = [{start=new DateTime(2018,9,21);``end``=new DateTime(2018,9,24)}]
    let sStart = Nullable (new DateTime(2018,9,13))
    let sEnd   = Nullable (new DateTime(2018,9,24))
    
    Assert.Equal(42.0,Capacity.GetUserCapacity(m,sStart,sEnd,t))

[<Fact>]
let ``middle saturday teamDayOff`` () =

    let m = {defaultMCR with daysOff=[|{start=new DateTime(2018,1,3);
                                        ``end``=new DateTime(2018,1,3)}|]}
    let t = [{start=new DateTime(2018,9,22);``end``=new DateTime(2018,9,22)}]
    let sStart = Nullable (new DateTime(2018,9,13))
    let sEnd   = Nullable (new DateTime(2018,9,24))
    
    Assert.Equal(56.0,Capacity.GetUserCapacity(m,sStart,sEnd,t))

[<Fact>]
let ``day user DayOff`` () =

    let m = {defaultMCR with daysOff=[|{start=new DateTime(2018,9,12);``end``=new DateTime(2018,9,14)}|]}
    let t = List.empty<DaysOff>
    let sStart = Nullable (new DateTime(2018,9,13))
    let sEnd   = Nullable (new DateTime(2018,9,24))
    
    Assert.Equal(42.0,Capacity.GetUserCapacity(m,sStart,sEnd,t))

[<Fact>]
let ``day multiple user DayOff`` () =

    let m = {defaultMCR with daysOff=[|{start=new DateTime(2018,9,12);``end``=new DateTime(2018,9,14)} 
                                       {start=new DateTime(2018,9,24);``end``=new DateTime(2018,9,24)}|]}
    let t = List.empty<DaysOff>
    let sStart = Nullable (new DateTime(2018,9,13))
    let sEnd   = Nullable (new DateTime(2018,9,24))
    
    Assert.Equal(35.0,Capacity.GetUserCapacity(m,sStart,sEnd,t))

[<Fact>]
let ``day windowing check`` () =

    let m = {defaultMCR with daysOff=[|{start=new DateTime(2018,9,12);``end``=new DateTime(2018,9,14)} 
                                       {start=new DateTime(2018,9,24);``end``=new DateTime(2018,9,24)}|]}
    let t = List.empty<DaysOff>
    let sStart = Nullable (new DateTime(2018,9,13))

    Assert.Equal(0.0,Capacity.GetUserCapacity(m,sStart,Nullable (new DateTime(2018,9,13)),t))
    Assert.Equal(0.0,Capacity.GetUserCapacity(m,sStart,Nullable (new DateTime(2018,9,14)),t))
    Assert.Equal(0.0,Capacity.GetUserCapacity(m,sStart,Nullable (new DateTime(2018,9,15)),t))
    Assert.Equal(0.0,Capacity.GetUserCapacity(m,sStart,Nullable (new DateTime(2018,9,16)),t))
    Assert.Equal(7.0,Capacity.GetUserCapacity(m,sStart,Nullable (new DateTime(2018,9,17)),t))
    Assert.Equal(14.0,Capacity.GetUserCapacity(m,sStart,Nullable (new DateTime(2018,9,18)),t))
    Assert.Equal(21.0,Capacity.GetUserCapacity(m,sStart,Nullable (new DateTime(2018,9,19)),t))
    Assert.Equal(28.0,Capacity.GetUserCapacity(m,sStart,Nullable (new DateTime(2018,9,20)),t))
    Assert.Equal(35.0,Capacity.GetUserCapacity(m,sStart,Nullable (new DateTime(2018,9,21)),t))
    Assert.Equal(35.0,Capacity.GetUserCapacity(m,sStart,Nullable (new DateTime(2018,9,22)),t))
    Assert.Equal(35.0,Capacity.GetUserCapacity(m,sStart,Nullable (new DateTime(2018,9,23)),t))
    Assert.Equal(35.0,Capacity.GetUserCapacity(m,sStart,Nullable (new DateTime(2018,9,24)),t))

