# Hướng dẫn Cài đặt DOTS Packages

## Các Packages đã được thêm vào manifest.json

Các packages sau đã được tự động thêm vào `Packages/manifest.json`:

1. **com.unity.entities** (1.3.5) - ECS Core (bao gồm Transforms)
2. **com.unity.physics** (1.3.5) - Unity Physics (hỗ trợ cả 2D và 3D)
3. **com.unity.burst** (1.8.18) - Burst Compiler
4. **com.unity.collections** (2.5.1) - Native Collections
5. **com.unity.mathematics** (1.3.2) - Unity Mathematics

**Lưu ý**: 
- `com.unity.transforms` được tích hợp sẵn trong `com.unity.entities`
- `com.unity.physics2d` không tồn tại như package riêng; Physics 2D được xử lý bởi `com.unity.physics`

## Các bước tiếp theo

1. **Mở Unity Editor**: Unity sẽ tự động phát hiện và bắt đầu download các packages mới.

2. **Chờ import hoàn tất**: Unity sẽ import các packages vào project. Quá trình này có thể mất vài phút.

3. **Kiểm tra lỗi**: Sau khi import xong, kiểm tra Console để đảm bảo không có lỗi.

4. **Verify packages**: Vào `Window > Package Manager` và kiểm tra các packages đã được cài đặt:
   - Entities (bao gồm Transforms)
   - Physics
   - Burst
   - Collections
   - Mathematics

## Nếu gặp lỗi

### Lỗi: "Package not found"
- Đảm bảo bạn đang sử dụng Unity 2023.x hoặc phiên bản tương thích
- Kiểm tra kết nối internet
- Thử xóa `Packages/packages-lock.json` và để Unity tải lại

### Lỗi: "Version mismatch"
- Các version trong manifest.json có thể cần điều chỉnh theo phiên bản Unity của bạn
- Kiểm tra Package Manager để xem version nào tương thích

### Lỗi: "Assembly reference missing"
- Đảm bảo tất cả packages đã được import hoàn tất
- Thử `Assets > Reimport All`
- Restart Unity Editor

## Sử dụng DOTS Systems

Sau khi packages đã được cài đặt, bạn có thể:

1. **Sử dụng ECS Systems**: Các systems trong `Assets/Scripts/DOTS/Systems/` sẽ tự động chạy trong Unity.

2. **Tạo Entities**: Sử dụng `CreatureDOTSAdapter` để chuyển đổi từ MonoBehaviour sang Entity.

3. **Sử dụng Speciation & Epigenetics**: Các systems này không cần DOTS packages, có thể sử dụng ngay.

## Lưu ý

- Hệ thống hiện tại vẫn sử dụng MonoBehaviour và sẽ tiếp tục hoạt động bình thường.
- DOTS implementation là một hệ thống song song, không thay thế hệ thống hiện tại.
- Bạn có thể chuyển đổi dần dần từ MonoBehaviour sang ECS khi sẵn sàng.

