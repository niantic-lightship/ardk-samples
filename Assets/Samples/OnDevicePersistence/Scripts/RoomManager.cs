using System;
using System.Collections.Generic;
using Niantic.Lightship.SharedAR.Datastore;
using Niantic.Lightship.SharedAR.Rooms;
using UnityEngine;

//JS: lifted from samples
public class RoomManager : MonoBehaviour
{
    /// <summary>
    /// Default Room description string
    /// </summary>
    public string _roomDescription;

    /// <summary>
    /// Default Room capacity
    /// </summary>
    [SerializeField]
    private int _roomCapacity = 10;

    /// <summary>
    /// Room object. Can be null before CreateNewRoom() or JoinRoomById() is called
    /// </summary>
    public IRoom Room { get; private set; }

    /// <summary>
    /// An event when Room is initialized and Networking/Datastore objects are ready to set event listeners
    /// </summary>
    public event Action<IRoom> OnRoomInitialized;

    private readonly List<string> _roomIdsToDelete = new List<string>();

    /// <summary>
    /// Create a new Room
    /// </summary>
    /// <param name="roomName">Name of the Room to create. No need to be unique name.</param>
    /// <param name="deleteRoomOnQuit">Delete the room when quit or not</param>
    public virtual void CreateNewRoom(string roomName, bool deleteRoomOnQuit = false)
    {
        var roomParams = new RoomParams(
            _roomCapacity,
            roomName,
            _roomDescription
        );

        // TODO: For now, room from CreateRoom() cannot be found in GetOrCreate
        var status = RoomManagementService.CreateRoom(roomParams, out var room);
        if (status != RoomManagementServiceStatus.Ok)
        {
            Debug.LogWarning($"Error in join room by name {roomName}, {status}");
            return;
        }
        Debug.Log($"ROOMID:{room.RoomParams.RoomID}");

        if (deleteRoomOnQuit)
        {
            _roomIdsToDelete.Add(room.RoomParams.RoomID);
        }

        Room = room;
        Room.Initialize();
        PostRoomInitialization();
        OnRoomInitialized?.Invoke(Room);
    }

    /// <summary>
    /// Join a Room with Room ID
    /// </summary>
    /// <param name="roomId">Room ID string</param>
    public virtual void JoinRoomById(string roomId)
    {
        var status = RoomManagementService.GetRoom(roomId, out var room);
        if (status != RoomManagementServiceStatus.Ok)
        {
            Debug.LogWarning($"Error in join room by id {roomId}, {status}");
            return;
        }
        Debug.Log($"ROOMID:{room.RoomParams.RoomID}");
        Room = room;
        Room.Initialize();
        PostRoomInitialization();
        OnRoomInitialized?.Invoke(Room);
    }
    
    public virtual void JoinRoomByName(string name)
    {
        var roomParams = new RoomParams(
            _roomCapacity,
            name,
            _roomDescription
        );
        var status = RoomManagementService.GetOrCreateRoomForName(roomParams, out var room);
            //RoomManagementService.GetRoom(roomId, out var room);
        if (status != RoomManagementServiceStatus.Ok)
        {
            Debug.LogWarning($"Error in join room by id {room?.RoomParams.RoomID}, {status}");
            return;
        }
        Debug.Log($"ROOMID:{room?.RoomParams.RoomID}");
        Room = room;
        Room.Initialize();
        PostRoomInitialization();
        OnRoomInitialized?.Invoke(Room);
    }

    public void LeaveRoom()
    {
        // No guarantee that network requests will succeed before shutdown
        // This is best effort for now, needs to be explicitly cleaned before shutdown for
        //  better reliability
        Room?.Leave();

        // TODO: check if no one else in the room before deleting it
        foreach (var roomId in _roomIdsToDelete)
        {
            var res = RoomManagementService.DeleteRoom(roomId);
            if (res != RoomManagementServiceStatus.Ok)
            {
                Debug.LogWarning($"Failed to delete room {roomId} with status {res}");
            }
            else
            {
                Debug.Log($"Deleted room: {roomId}");
            }
        }
    }

    protected void OnDestroy()
    {
        LeaveRoom();
    }
    
    /// <summary>
    /// Transport specific logic after Room initialization
    /// No implementation for this class
    /// </summary>
    protected virtual void PostRoomInitialization(){}
}