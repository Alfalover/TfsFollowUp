module Capacity

    open Tfs
    open Update
    open System
    open Session
    open TimeSeries

    let GetDaysSeq(dStart:DateTime,dEnd) = 
        let days = (int)((dEnd-dStart).TotalDays)
        seq { for i in 0 .. days do yield dStart.AddDays((float)i) }
    
    let GetUserFactor (user:TeamMember) (factors:memberFactorList) : double =
             factors.value |> Array.tryFind(fun x -> x.teamMemberId = user.id)
                           |> function 
                              | Some x -> x.factor  
                              | None -> 0.0


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
        Factor : double
    }

    type MemberStats = {

        User: TeamMember
        Stats: capacityStats
    }

    type MemberStatsSeries = {
        User: TeamMember
        RemCap : datapoint list
    }


    
    type CapacityService(upd : UpdateService) = 

        let MinDates dx dy = if dx > dy then dy else dx

        member this.GetCapacitySerie (sprint:Sprint) = 
               let data = upd.GetMemberCapacities sprint.id
               let teamdaysoff = (upd.GetTeamCapacities sprint.id).daysOff |> List.ofArray

               let totalDays = (sprint.attributes.finishDate.Value-sprint.attributes.startDate.Value).Days + 1;
               let startDate = sprint.attributes.startDate.Value
               let endDate   = sprint.attributes.finishDate.Value
               let days = seq { for i in 0 .. totalDays -> startDate.AddDays(float(i)) }

               let members = data.value |> List.ofArray
                                        |> List.map(fun x -> {User = x.teamMember
                                                              RemCap = days |> Seq.map ( fun y ->
                                                                                            {t = DateTimeOffset(y.AddDays(-1.0))
                                                                                             y = GetUserCapacity(x,Nullable(startDate),Nullable(endDate),teamdaysoff)
                                                                                                  - GetUserCapacity(x,Nullable(startDate),Nullable(y.AddDays(-1.0)),teamdaysoff)
                                                                                            })
                                                                            |> List.ofSeq
                                                              })
               members                                                                
    
        member this.GetTeamMembersWork (sprint: Sprint) =
               let data = upd.GetMemberCapacities sprint.id
               let factors = upd.GetTeamFactors sprint.id
               let teamdaysoff = (upd.GetTeamCapacities sprint.id).daysOff |> List.ofArray
               
               let members = data.value |> List.ofArray
                                        |> List.map(fun x -> {User = x.teamMember
                                                              Stats = {CapacityDay = x.activities.[0].capacityPerDay
                                                                       CapacitySprint = GetUserCapacity(x,sprint.attributes.startDate,sprint.attributes.finishDate,teamdaysoff)
                                                                       CapacityUntilToday = GetUserCapacity(x,sprint.attributes.startDate,Nullable (MinDates (DateTime.Today.AddDays(-1.0)) sprint.attributes.finishDate.Value),teamdaysoff)
                                                                       Factor = GetUserFactor x.teamMember factors }})
     
               members
        
        member this.GetTeamWork (sprint: Sprint) =
               
               this.GetTeamMembersWork sprint
               |> List.groupBy (fun x -> true)
               |> List.map (fun (x,y) -> {
                                        CapacityDay = y |> List.sumBy(fun z -> z.Stats.CapacityDay)
                                        CapacitySprint = y |> List.sumBy(fun z -> z.Stats.CapacitySprint)
                                        CapacityUntilToday = y |> List.sumBy(fun z -> z.Stats.CapacityUntilToday)
                                        Factor = y |> List.averageBy(fun z -> z.Stats.Factor)
                                     })

