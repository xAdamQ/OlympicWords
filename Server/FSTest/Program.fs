open System

// For more information see https://aka.ms/fsharp-console-apps
//let n1 = int (Console.ReadLine())
//let n2 = int (Console.ReadLine())
//
//if n1 > n2 then
//    printfn "it is bigger"
//else
//    printfn "it is smaller"

//let msg =
//    if n1 > n2 then "bigger"
//    elif n1 = n2 then "equal"
//    else "smaller"
//Console.WriteLine msg

for n in [ 1; 2; 3; 4 ] do
    Console.WriteLine n

for i = 0 to 5 do
    Console.WriteLine i

let add10 n = n + 10

let a = add10 21

let toInt (a: string) (b: string) = int a + int b

let aa = toInt "23" "66"
Console.WriteLine aa

//pipeline
let y = 4
let pwr a = a * a

let nn = y |> pwr |> add10
Console.WriteLine nn

//composition
let pwrPlus = pwr >> add10
let nn2 = pwrPlus y
Console.WriteLine nn2

//LIST
let l1 = [ 33; 55; 88 ]
//append item at the start
let l2 = 99 :: l1
//concatl lists
let l3 = l2 @ l1
let l4 = l2 |> List.append [ 99 ]

Console.WriteLine(l3.Item 2)

Console.WriteLine "------"

for n in l2 do
    Console.WriteLine n

Console.WriteLine "------"

for n in l3 do
    Console.WriteLine n

let a1 = string Console.ReadLine
let tstFun = printfn "hello world: %s" a1

tstFun

let a2: double = 13

let applyFunction (f: int -> int -> int) x y = f x y
//this line takes a function of this signature and x, y variables

let res = applyFunction (fun x y -> x * y) 5 7
//pass lambda expression and args to applyFunction

let a3 =
    """ I can use special "wow" chars here?  """

let squareChar s = String.collect (fun c -> $"[%c{c}] ") s
Console.WriteLine(squareChar "I will das Besteck putzen")

let sArr = [ "wir"; "konnen"; "nicht"; "bleiben" ]
Console.WriteLine(String.concat " ## " sArr)

(printfn "%s") <| String.replicate 10 "*! "

let safeDiv x y =
    match y with
    | 0 -> None
    | _ -> Some(x / y)

Console.WriteLine(safeDiv 22 11)

Console.WriteLine(
    if (safeDiv 22 0) = None then
        "not good"
    else
        "good"
)


let tupleFun (a, b) = a ** b

Console.WriteLine(tupleFun (3., 4.))

type person = string * DateTime
type employee = person * int

//let messi =
//    "messi"
//    DateTime.Now
//}
//
//let Nymar: employee =
//    { name = "ny"
//      salary = 80
//      birth_date = DateTime.Now }

type tstClass =
    member this.Name = "unnamed"

//CHANGING THE HASHCODE OF AN OBJECT IN C# WILL MAKE IT A DIFFERENT OBJECT
