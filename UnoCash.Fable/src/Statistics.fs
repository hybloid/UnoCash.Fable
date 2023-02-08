module UnoCash.Fulma.Statistics

open Fable.Import
open UnoCash.Fulma.Helpers
open UnoCash.Fulma.Models
open UnoCash.Fulma.Messages
open Feliz
open Feliz.Recharts
open System
open Fable.React.Helpers

// Rebuild financial dashboard here

// Generalize expensesTable from ShowExpenses module

type private PolarData =
    { name: string
      value: int }

type private IPolarPayload =
    abstract fill: string with get, set
    abstract name: string with get, set
    abstract strock: string with get, set
    abstract value: int with get, set

type private IPolarProps =
    abstract midAngle: float with get, set
    abstract startAngle: float with get, set
    abstract endAngle: float with get, set
    abstract outerRadius: float with get, set
    abstract innerRadius: float with get, set
    abstract cx: float with get, set
    abstract cy: float with get, set
    abstract fill: string with get, set
    abstract payload: IPolarPayload with get, set
    abstract percent: float with get, set
    abstract value: float with get, set

let private colors =
    [ "#0088FE"; "#00C49F"; "#FFBB28"; "#FF8042" ]

let private polarDataInnerCircle count =
    let positive =
        if count > 0 then count else 100

    let negative =
        if count < 0 then count * -1 else 100

    [| { name = "Positive"
         value = positive }
       { name = "Negative"
         value = negative } |]

let private onPieEnter polarData dispatch (data: IPolarProps) =
    // This is crap, Fable.Recharts has the index in the arguments
    // of the event, there must be a way to get that here, too
    polarData |>
    Array.findIndex (fun x -> x.value = data.payload.value) |>
    ChangePieChartIndex |>
    dispatch

let private renderActiveShape (data: IPolarProps) =
    let radian = Math.PI / 180.
    let sin = Math.Sin(-radian * data.midAngle)
    let cos = Math.Cos(-radian * data.midAngle)
    let sx = data.cx + (data.outerRadius + 10.) * cos
    let sy = data.cy + (data.outerRadius + 10.) * sin
    let mx = data.cx + (data.outerRadius + 30.) * cos
    let my = data.cy + (data.outerRadius + 30.) * sin

    let ex =
        mx + (if cos >= 0. then 1. else -1.0)
             * 22.

    let ey =
        my

    let textAnchor =
        if cos >= 0. then svg.textAnchor.startOfText else svg.textAnchor.endOfText

    Svg.g
        [ Svg.text
              [ svg.cx data.cx
                svg.y data.cy
                svg.textAnchor.middle
                svg.fill data.fill
                svg.dy 8
                svg.children [ str data.payload.name ] ]

          Recharts.sector
              [ prop.cx data.cx |> unbox<ISectorProperty>
                prop.cy data.cy |> unbox<ISectorProperty>

                pie.startAngle data.startAngle    |> unbox<ISectorProperty>
                pie.endAngle data.endAngle        |> unbox<ISectorProperty>
                pie.innerRadius data.innerRadius  |> unbox<ISectorProperty>
                pie.outerRadius data.outerRadius  |> unbox<ISectorProperty>
                Interop.mkAttr "fill" "#444"      |> unbox<ISectorProperty> ]    

          Recharts.sector
              [ prop.cx data.cx                          |> unbox<ISectorProperty> 
                prop.cy data.cy                          |> unbox<ISectorProperty> 
                                                         |> unbox<ISectorProperty> 
                pie.startAngle data.startAngle           |> unbox<ISectorProperty> 
                pie.endAngle data.endAngle               |> unbox<ISectorProperty> 
                pie.innerRadius (data.outerRadius + 6.)  |> unbox<ISectorProperty> 
                pie.outerRadius (data.outerRadius + 10.) |> unbox<ISectorProperty> 
                Interop.mkAttr "fill" "#222"             |> unbox<ISectorProperty> ]

          Svg.path
              [ svg.d (sprintf "M%f,%fL%f,%fL%f,%f" sx sy mx my ex ey)
                svg.stroke data.fill
                svg.fill "none" ]

          Svg.circle
              [ svg.cx ex
                svg.cy ey
                svg.r 2
                svg.fill data.fill
                svg.stroke "none" ]

          Svg.text
              [ svg.x (ex + (if cos >= 0. then 1. else -1.0) * 12.)
                svg.y ey
                textAnchor
                svg.fill "#333"
                svg.children [ str (sprintf "£%.0f" data.value) ] ]

          Svg.text
              [ svg.x (ex + (if cos >= 0. then 1. else -1.0) * 12.)
                svg.y ey
                textAnchor
                svg.fill "#999"
                svg.dy 18
                svg.children [ str (sprintf "(%.2f%%)" (data.percent * 100.)) ] ] ]

let private totalsPieChart state dispatch =
    let counter = 1
    
    // How do we allocate the percentages when a transaction has multiple tags?
    let data =
        state.Expenses |>
        Array.groupBy (fun ex -> ex.tags.Split(',') |>
                                 Array.filter (fun x -> match x.Trim() with | "" -> false | _ -> true) |>
                                 Array.tryHead |>
                                 Option.defaultValue "other") |>
        Array.map (fun (tag, exs) -> { name = tag; value = (exs |> Array.sumBy (fun x -> int x.amount)) }) |>
        Array.sortBy (fun x -> x.value)

    Recharts.pieChart
        [ pieChart.margin (10, 30, 0, 0)
          pieChart.width (int Browser.Dom.window.innerWidth)
          pieChart.height 300
          pieChart.children
              [ Recharts.pie
                  [ pie.data data
                    pie.dataKey "value"
                    pie.nameKey "name"
                    pie.label false
                    pie.innerRadius 60
                    pie.outerRadius 80
                    pie.activeIndex state.PieChartIndex
                    
                    Interop.mkPieAttr "activeShape" renderActiveShape
                    
                    pie.children [
                        Recharts.cell [ cell.fill "#0088FE" ]
                        Recharts.cell [ cell.fill "#00C49F" ]
                        Recharts.cell [ cell.fill "#FFBB28" ]
                        Recharts.cell [ cell.fill "#FF8042" ]
                    ]
                    
                    Interop.mkPieAttr "onMouseEnter" (onPieEnter data dispatch) ]

                Recharts.pie
                    [ pie.data (polarDataInnerCircle counter)
                      pie.dataKey "value"
                      pie.nameKey "name"
                      pie.label false
                      pie.innerRadius 45
                      pie.outerRadius 50
                      pie.fill "#82ca9d"
                      Interop.mkPieAttr "fill" (colors.[(abs counter) % colors.Length]) ] ] ]

let statisticsCard model dispatch =
    card [ totalsPieChart model dispatch ]
         Html.none