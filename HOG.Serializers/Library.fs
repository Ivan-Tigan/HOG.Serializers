module Serializers

open System.Threading
open MBrace.FsPickler
open Newtonsoft.Json

type Serializer<'a> = {serialize:'a -> byte []; deserialize: byte[] -> 'a}
    with member this.round_trip = this.serialize >> this.deserialize

let private json_ser_settings =
    let set = new JsonSerializerSettings()
    set.ConstructorHandling <- ConstructorHandling.AllowNonPublicDefaultConstructor
    set
let test_round_trip (ser:Serializer<'a>) (a:'a) = a = ser.round_trip a
[<CustomPickler>]
type SMutex = private SMutex of Mutex with
    static member create () = SMutex(new Mutex()) 
    member this.WaitOne() = match this with SMutex m -> m.WaitOne()
    member this.ReleaseMutex() = match this with SMutex m -> m.ReleaseMutex()
    static member CreatePickler (resolver:IPicklerResolver) = Pickler.FromPrimitives((fun _ -> SMutex (new Mutex())),  fun _ _ -> ())
FsPickler.GeneratePickler<SMutex>()

let json_serializer<'a> = { serialize = (fun (a:'a) -> a |> (fun x -> JsonConvert.SerializeObject(x, json_ser_settings)) |> System.Text.Encoding.UTF8.GetBytes); deserialize = System.Text.Encoding.UTF8.GetString >> (fun s -> JsonConvert.DeserializeObject<'a>(s, json_ser_settings))}

let private pickler = FsPickler.CreateBinarySerializer()
let pickle_serializer<'a> = { serialize = pickler.Pickle; deserialize = pickler.UnPickle<'a>}
