using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Text;
using UnityEngine;

public class DistanceResult {
	public Vector3 position;
	public float first;
	public float last;
	public float angle;
}

public class SF45Device {
	public enum RecvState {
		WAIT_FOR_START,
		WAIT_FOR_FLAGS,
		WAIT_FOR_PAYLOAD,
		WAIT_FOR_CHECKSUM,
	};

	const byte FRAME_START = 0xAA;
	
	private SerialPort _serial;

	private byte[] _recvBuffer = new byte[1200];
	private int _recvLen = 0;
	private int _recvPayloadSize = 0;
	private UInt16 _recvFlags = 0;
	private int _recvWriteFlag = 0;
	private UInt16 _recvChecksum = 0;
	private RecvState _recvState = RecvState.WAIT_FOR_START;

	public List<DistanceResult> distanceResults = new List<DistanceResult>();
	
	public void Connect(string Port, int Baudrate) {
		Debug.Log("Device connecting.");
		_serial = new SerialPort("\\\\.\\" + Port, Baudrate);

		try {
			_serial.Open();
			_serial.DtrEnable = true;
			_serial.DiscardInBuffer();
			Debug.Log("Device connected.");
		} catch (Exception Ex) {
			Debug.LogError("Device serial connect failed: " + Ex.Message);
		}
	}

	public UInt16 CreateCrc(byte[] Data, int Offset, int Size) {
		UInt16 crc = 0;

		for (int i = Offset; i < Size; ++i) {
			UInt16 code = (UInt16)(crc >> 8);
			code ^= Data[i];
			code ^= (UInt16)(code >> 4);
			crc = (UInt16)(crc << 8);
			crc ^= code;
			code = (UInt16)(code << 5);
			crc ^= code;
			code = (UInt16)(code << 7);
			crc ^= code;
		}

		return crc;
	}

	public void SendGetProduct() {
		BinaryWriter w = new BinaryWriter(new MemoryStream(1200));

		w.Write(FRAME_START);
		UInt16 flags = 0;
		int payloadLength = 1;
		int write = 0;
		flags |= (UInt16)(payloadLength << 6);
		flags |= (UInt16)write;
		w.Write(flags);
		w.Write((byte)0);
		UInt16 checksum = CreateCrc(((MemoryStream)w.BaseStream).GetBuffer(), 0, (int)w.BaseStream.Length);
		w.Write(checksum);

		_serial.Write(((MemoryStream)w.BaseStream).GetBuffer(), 0, (int)w.BaseStream.Length);
	}

	public void SendWriteInt8(int Opcode, byte Value) {
		BinaryWriter w = new BinaryWriter(new MemoryStream(1200));

		w.Write(FRAME_START);
		UInt16 flags = 0;
		int payloadLength = 2;
		int write = 1;
		flags |= (UInt16)(payloadLength << 6);
		flags |= (UInt16)write;
		w.Write(flags);
		w.Write((byte)Opcode);
		w.Write(Value);
		UInt16 checksum = CreateCrc(((MemoryStream)w.BaseStream).GetBuffer(), 0, (int)w.BaseStream.Length);
		w.Write(checksum);

		_serial.Write(((MemoryStream)w.BaseStream).GetBuffer(), 0, (int)w.BaseStream.Length);
	}

	public void SendWriteInt16(int Opcode, Int16 Value) {
		BinaryWriter w = new BinaryWriter(new MemoryStream(1200));

		w.Write(FRAME_START);
		UInt16 flags = 0;
		int payloadLength = 4;
		int write = 1;
		flags |= (UInt16)(payloadLength << 6);
		flags |= (UInt16)write;
		w.Write(flags);
		w.Write((byte)Opcode);
		w.Write(Value);
		UInt16 checksum = CreateCrc(((MemoryStream)w.BaseStream).GetBuffer(), 0, (int)w.BaseStream.Length);
		w.Write(checksum);

		_serial.Write(((MemoryStream)w.BaseStream).GetBuffer(), 0, (int)w.BaseStream.Length);
	}

	public void SendWriteInt32(int Opcode, int Value) {
		BinaryWriter w = new BinaryWriter(new MemoryStream(1200));

		w.Write(FRAME_START);
		UInt16 flags = 0;
		int payloadLength = 5;
		int write = 1;
		flags |= (UInt16)(payloadLength << 6);
		flags |= (UInt16)write;
		w.Write(flags);
		w.Write((byte)Opcode);
		w.Write(Value);
		UInt16 checksum = CreateCrc(((MemoryStream)w.BaseStream).GetBuffer(), 0, (int)w.BaseStream.Length);
		w.Write(checksum);

		_serial.Write(((MemoryStream)w.BaseStream).GetBuffer(), 0, (int)w.BaseStream.Length);
	}

	public void SendWriteFloat(int Opcode, float Value) {
		BinaryWriter w = new BinaryWriter(new MemoryStream(1200));

		w.Write(FRAME_START);
		UInt16 flags = 0;
		int payloadLength = 5;
		int write = 1;
		flags |= (UInt16)(payloadLength << 6);
		flags |= (UInt16)write;
		w.Write(flags);
		w.Write((byte)Opcode);
		w.Write(Value);
		UInt16 checksum = CreateCrc(((MemoryStream)w.BaseStream).GetBuffer(), 0, (int)w.BaseStream.Length);
		w.Write(checksum);

		_serial.Write(((MemoryStream)w.BaseStream).GetBuffer(), 0, (int)w.BaseStream.Length);
	}

	public void SendScanEnable() {
		SendWriteInt8(96, 1);
	}

	public void SendScanDisable() {
		SendWriteInt8(96, 0);
	}

	public void SendWritePosition(float Degrees) {
		SendWriteFloat(97, Degrees);
	}

	public List<DistanceResult> PopDistanceResults() {
		List<DistanceResult> temp = distanceResults;
		distanceResults = new List<DistanceResult>();

		return temp;
	}

	private void _ProcessSignalData(byte[] Data, int DataLen) {
		BinaryReader r = new BinaryReader(new MemoryStream(Data, 4, DataLen));

		DistanceResult result = new DistanceResult();
		distanceResults.Add(result);

		result.first = (float)r.ReadUInt16() / 100.0f;
		result.last	= (float)r.ReadUInt16() / 100.0f;
		result.angle = (float)r.ReadInt16() / 100.0f;
		
		// Determine cartesian co-ordinates.
		float rad = result.angle * (Mathf.PI / 180.0f);

		Vector3 position = new Vector3();
		position.x = Mathf.Sin(rad) * result.first;
		position.y = 0.0f;
		position.z = Mathf.Cos(rad) * result.first;

		result.position = position;
	}

	private void _ProcessPacket(byte[] Data, int PayloadLen) {
		if (PayloadLen == 0) {
			return;
		}

		int offset = 3;

		int opcode = Data[offset];
		int dataLen = PayloadLen - 1;
		offset += 1;
		
		if (opcode == 0 && dataLen == 16) {
			// 0. Product name.
			string productName = Encoding.UTF8.GetString(Data, offset, dataLen);
			Debug.Log("Product name: " + productName);
		} else if (opcode == 44) {
			// 44. Distance data in cm.
			_ProcessSignalData(Data, dataLen);
		}
	}

	public void Update() {
		if (_serial != null && _serial.IsOpen) {

			while (true) {
				int bytesReady = _serial.BytesToRead;

				if (bytesReady == 0) {
					break;
				}

				byte[] dataBuf = new byte[bytesReady];
				_serial.Read(dataBuf, 0, bytesReady);
				//Debug.Log("Read " + bytesReady + " bytes");

				for (int i = 0; i < dataBuf.Length; ++i) {
					byte b = dataBuf[i];

					if (_recvState == RecvState.WAIT_FOR_START) {
						if (b == FRAME_START) {
							_recvState = RecvState.WAIT_FOR_FLAGS;
							_recvLen = 0;
							_recvBuffer[_recvLen++] = b;
						}
					} else if (_recvState == RecvState.WAIT_FOR_FLAGS) {
						_recvBuffer[_recvLen++] = b;

						if (_recvLen == 3) {
							_recvFlags = (UInt16)((UInt16)(_recvBuffer[1]) | (UInt16)(_recvBuffer[2] << 8));
							_recvPayloadSize = _recvFlags >> 6;
							_recvWriteFlag = _recvFlags & 0x1;
							// Debug.Log("Packet - size: " + _recvPayloadSize + " write: " + _recvWriteFlag);

							if (_recvPayloadSize < 0 || _recvPayloadSize > 1023) {
								Debug.LogError("Packet size invalid: " + _recvPayloadSize);
								_recvState = RecvState.WAIT_FOR_START;
							} else {
								_recvState = RecvState.WAIT_FOR_PAYLOAD;
							}
						}
					} else if (_recvState == RecvState.WAIT_FOR_PAYLOAD) {
						_recvBuffer[_recvLen++] = b;

						if (_recvLen == _recvPayloadSize + 3) {
							_recvState = RecvState.WAIT_FOR_CHECKSUM;
						}
					} else if (_recvState == RecvState.WAIT_FOR_CHECKSUM) {
						_recvBuffer[_recvLen++] = b;

						if (_recvLen == _recvPayloadSize + 5) {
							_recvChecksum = (UInt16)((UInt16)(_recvBuffer[_recvLen - 2]) | (UInt16)(_recvBuffer[_recvLen - 1] << 8));
							UInt16 actualChecksum = CreateCrc(_recvBuffer, 0, _recvLen - 2);
							// Debug.Log("Checksums - " + actualChecksum + " : " + _recvChecksum);

							if (actualChecksum != _recvChecksum) {
								Debug.LogError("Packet checksum failed");
							} else {
								_ProcessPacket(_recvBuffer, _recvPayloadSize);
							}
							
							_recvState = RecvState.WAIT_FOR_START;
						}
					}
				}
			}
		}
	}
}
