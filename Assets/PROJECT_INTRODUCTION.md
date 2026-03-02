# GIỚI THIỆU DỰ ÁN VERRARIUM

## Tóm tắt Dự án

**Verrarium** là một hệ thống giả lập sự sống nhân tạo (Artificial Life) thời gian thực được phát triển bằng Unity 2D, sử dụng thuật toán tiến hóa mạng nơ-ron **rtNEAT (real-time NeuroEvolution of Augmenting Topologies)** để tạo ra một "vườn thú ảo" nơi các sinh vật sống, tiến hóa và tương tác với nhau trong một môi trường mô phỏng.

Dự án kết hợp ba yếu tố chính: **Artificial Life** (tạo ra hệ sinh thái nhân tạo với các sinh vật có vòng đời đầy đủ), **Tính toán Tiến hóa** (sử dụng rtNEAT để tiến hóa cả cấu trúc và trọng số của mạng nơ-ron điều khiển hành vi), và **Giải trí/Giáo dục** (tạo ra một sản phẩm tương tác cho phép người dùng quan sát và học hỏi về quá trình tiến hóa).

## Đặc điểm Nổi bật

- **Tiến hóa Thời gian Thực**: Sử dụng biến thể rtNEAT để tiến hóa mạng nơ-ron liên tục, không có thế hệ rõ ràng, với chọn lọc tự nhiên thuần túy (không có hàm fitness tường minh).

- **Hệ thống Sinh học Phức tạp**: Mỗi sinh vật có vòng đời đầy đủ (sinh, tăng trưởng, sinh sản, chết), hệ thống trao đổi chất và năng lượng, cơ chế đói (starvation), hệ thống miệng (mouth system) cho phép ăn có hướng, và cơ chế lão hóa.

- **Performance Tối ưu**: Áp dụng time-slicing cho neural network computation và spatial partitioning cho spatial queries, cho phép simulation chạy mượt mà với 200+ sinh vật ở 60 FPS.

- **Tính năng Đầy đủ**: Hệ thống save/load với JSON serialization, pause menu, autosave, và giao diện người dùng trực quan cho phép quan sát và điều chỉnh simulation trong thời gian thực.

## Mục tiêu

Dự án hướng tới ba mục tiêu chính: (1) **Nghiên cứu Artificial Life** - tạo ra môi trường mô phỏng nơi các sinh vật nhân tạo có thể tiến hóa thông qua chọn lọc tự nhiên, (2) **Áp dụng Tính toán Tiến hóa** - triển khai và tối ưu hóa thuật toán rtNEAT để tiến hóa hành vi phức tạp, và (3) **Tạo ra Sản phẩm Giải trí và Giáo dục** - phát triển một ứng dụng tương tác cho phép người dùng quan sát, tương tác và học hỏi về quá trình tiến hóa.

## Kết quả

Dự án đã đạt được các thành quả đáng kể: triển khai thành công rtNEAT với topology evolution và innovation tracking, xây dựng hệ thống sinh học phức tạp với vòng đời đầy đủ và các cơ chế sinh lý, tối ưu hóa performance đạt 60 FPS với 100+ sinh vật, và phát triển hệ thống save/load hoàn chỉnh. Hệ thống đã chứng minh tính khả thi của việc tạo ra Artificial Life simulation thời gian thực với tiến hóa mạng nơ-ron, và có tiềm năng trở thành một công cụ nghiên cứu và giáo dục quan trọng trong lĩnh vực Artificial Life và Neuroevolution.

---

## Abstract (English)

**Verrarium** is a real-time Artificial Life simulation system developed using Unity 2D, employing the **rtNEAT (real-time NeuroEvolution of Augmenting Topologies)** algorithm to create a "virtual zoo" where artificial creatures live, evolve, and interact within a simulated environment.

The project combines three main elements: **Artificial Life** (creating an artificial ecosystem with creatures having complete lifecycles), **Evolutionary Computation** (using rtNEAT to evolve both structure and weights of neural networks controlling behavior), and **Entertainment/Education** (developing an interactive application allowing users to observe and learn about the evolutionary process).

**Key Features**: Real-time evolution using rtNEAT variant, complex biological systems with complete lifecycles, optimized performance achieving 60 FPS with 200+ creatures, and comprehensive save/load system with JSON serialization.

**Results**: Successfully implemented rtNEAT with topology evolution, built complex biological systems, optimized performance, and developed a complete save/load system. The system demonstrates the feasibility of real-time Artificial Life simulation with neural network evolution and has potential as an important research and educational tool in Artificial Life and Neuroevolution.





