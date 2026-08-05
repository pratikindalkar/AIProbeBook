document.addEventListener("DOMContentLoaded", function () {

    // ✅ Safe TempData message
    var message = @Html.Raw(System.Text.Json.JsonSerializer.Serialize(TempData["Error"] ?? ""));

    if (message && message !== "") {
        Swal.fire({
            icon: 'error',
            title: message,
            showConfirmButton: false,
            timer: 3000
        });
    }

    // ✅ Inputs
    const usernameInput = document.getElementById("Login_Name");
    const typeInput = document.getElementById("typeInput");
    const passwordInput = document.getElementById("password");
    const passwordInput2 = document.getElementById("cpassword");

    const togglePassword = document.getElementById("togglePassword1");
    const togglePassword2 = document.getElementById("togglePassword2");

    let errorText = "";

    // ✅ 🔥 LOCAL STORAGE (KEEP DATA AFTER ERROR)
    loadSavedData();

    // ✅ Save on typing
    [usernameInput, typeInput, passwordInput, passwordInput2].forEach(input => {
        input.addEventListener("input", saveData);
    });

    function saveData() {
        sessionStorage.setItem("signupData", JSON.stringify({
            username: usernameInput.value,
            type: typeInput.value,
            password: passwordInput.value,
            cpassword: passwordInput2.value
        }));
    }

    function loadSavedData() {
        const data = JSON.parse(sessionStorage.getItem("signupData"));
        if (data) {
            usernameInput.value = data.username || "";
            typeInput.value = data.type || "";
            passwordInput.value = data.password || "";
            passwordInput2.value = data.cpassword || "";
        }
    }

    function clearSavedData() {
        sessionStorage.removeItem("signupData");
    }

    // ✅ Password toggle
    togglePassword.addEventListener("click", function () {
        passwordInput.type = passwordInput.type === "password" ? "text" : "password";
        this.classList.toggle("fa-eye-slash");
    });

    togglePassword2.addEventListener("click", function () {
        passwordInput2.type = passwordInput2.type === "password" ? "text" : "password";
        this.classList.toggle("fa-eye-slash");
    });

    // ✅ Validation
    window.checkPass = function () {
        const username = usernameInput.value;
        const password = passwordInput.value;
        const cpassword = passwordInput2.value;
        const typeInp = typeInput.value;

        if (username === "" || password === "" || cpassword === "" || typeInp === "") {
            Swal.fire({
                icon: 'warning',
                title: 'All fields are required'
            });
            return false;
        }

        if (password !== cpassword) {
            Swal.fire({
                icon: 'error',
                title: 'Passwords do not match'
            });
            return false;
        }

        // ✅ Clear storage on success
        clearSavedData();

        return true;
    };

    // ✅ Blur validation
    usernameInput.addEventListener("blur", function () {
        errorText = document.getElementById("usernameError");
        if (usernameInput.value.trim() === "") {
            usernameInput.classList.add("error");
            errorText.textContent = "Username is required";
        } else {
            usernameInput.classList.remove("error");
            errorText.textContent = "";
        }
    });

    typeInput.addEventListener("blur", function () {
        errorText = document.getElementById("typeError");
        if (typeInput.value === "") {
            typeInput.classList.add("error");
            errorText.textContent = "Type is required";
        } else {
            typeInput.classList.remove("error");
            errorText.textContent = "";
        }
    });

    passwordInput.addEventListener("blur", function () {
        errorText = document.getElementById("passwordError");
        if (passwordInput.value.trim() === "") {
            passwordInput.classList.add("error");
            errorText.textContent = "Password is required";
        } else {
            passwordInput.classList.remove("error");
            errorText.textContent = "";
        }
    });

    passwordInput2.addEventListener("blur", function () {
        errorText = document.getElementById("cpasswordError");
        if (passwordInput2.value.trim() === "") {
            passwordInput2.classList.add("error");
            errorText.textContent = "Confirm Password is required";
        } else {
            passwordInput2.classList.remove("error");
            errorText.textContent = "";
        }
    });

});