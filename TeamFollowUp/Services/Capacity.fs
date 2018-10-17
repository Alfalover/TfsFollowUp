﻿module Capacity

    open Tfs
    open Update
    open System
    open Session

    let GetDaysSeq(dStart:DateTime,dEnd) = 
        let days = (int)((dEnd-dStart).TotalDays)
        seq { for i in 0 .. days do yield dStart.AddDays((float)i) }
    
    let GetUserCapacity (m,sStart :Nullable<DateTime>,sEnd :Nullable<DateTime>,t) = 

           let cap = m.activities.[0].capacityPerDay;

           if sStart.HasValue && sEnd.HasValue then
                let workList = GetDaysSeq(sStart.Value,sEnd.Value)
                               |> Seq.filter(fun x -> x.DayOfWeek <> DayOfWeek.Saturday && x.DayOfWeek <> DayOfWeek.Sunday)
                
                let teamDaysOff = t |> List.map(fun x -> GetDaysSeq(x.start,x.``end``)) 
                                    |> Seq.concat

                let userDaysOff = m.daysOff
                                  |> List.ofArray
                                  |> List.map(fun x -> GetDaysSeq(x.start,x.``end``)) 
                                  |> Seq.concat

                let DaysOff = [teamDaysOff;userDaysOff] 
                                  |> Seq.ofList 
                                  |> Seq.concat

                let workList = workList |> Seq.filter(fun x -> not (DaysOff |> Seq.contains(x)))

                cap*(double)(Seq.length(workList))
           else 
                0.0

    type capacityStats = { 

        CapacityDay : double
        CapacitySprint: double
        CapacityUntilToday : double
    }

    type MemberStats = {

        User: TeamMember
        Stats: capacityStats
    }


    
    type CapacityService(upd : UpdateService) = 

    
        member this.GetTeamMembersWork (sprint: Sprint) =
               let data = upd.GetMemberCapacities sprint.id
               let teamdaysoff = (upd.GetTeamCapacities sprint.id).daysOff |> List.ofArray
               
               let members = data.value |> List.ofArray
                                        |> List.map(fun x -> {User = x.teamMember
                                                              Stats = {CapacityDay = x.activities.[0].capacityPerDay
                                                                       CapacitySprint = GetUserCapacity(x,sprint.attributes.startDate,sprint.attributes.finishDate,teamdaysoff)
                                                                       CapacityUntilToday = GetUserCapacity(x,sprint.attributes.startDate,Nullable (DateTime.Today.AddDays(-1.0)),teamdaysoff)}})
     
               members
        
        member this.GetTeamWork (sprint: Sprint) =
               
               this.GetTeamMembersWork sprint
               |> List.groupBy (fun x -> true)
               |> List.map (fun (x,y) -> {
                                        CapacityDay = y |> List.sumBy(fun z -> z.Stats.CapacityDay)
                                        CapacitySprint = y |> List.sumBy(fun z -> z.Stats.CapacitySprint)
                                        CapacityUntilToday = y |> List.sumBy(fun z -> z.Stats.CapacityUntilToday)
                                     })
