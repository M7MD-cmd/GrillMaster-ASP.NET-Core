// =========================================================
// ORDER FORM
// =========================================================

document.addEventListener("DOMContentLoaded", function () {

    const form = document.getElementById("orderForm");

    if (!form) return;


    // =========================================================
    // CHECKBOXES
    // =========================================================

    document.querySelectorAll("#meals input[type='checkbox']")
        .forEach((checkbox) => {

            checkbox.addEventListener("change", function () {

                const mealOption =
                    this.closest(".meal-option");

                if (!mealOption) return;

                const quantityBox =
                    mealOption.querySelector(".quantity-box");

                const quantityInput =
                    mealOption.querySelector(".meal-quantity");


                if (this.checked) {

                    mealOption.classList.add("selected");

                    if (quantityBox) {
                        quantityBox.hidden = false;
                    }

                    if (quantityInput) {

                        if (!quantityInput.value ||
                            parseInt(quantityInput.value) < 1) {

                            quantityInput.value = 1;
                        }

                    }

                }
                else {

                    mealOption.classList.remove("selected");

                    if (quantityBox) {
                        quantityBox.hidden = true;
                    }

                    if (quantityInput) {
                        quantityInput.value = 1;
                    }

                }

            });

        });


    // =========================================================
    // FORM SUBMIT
    // =========================================================

    form.addEventListener("submit", function (e) {

        e.preventDefault();


        // ---------------------------------------------------------
        // Customer information
        // ---------------------------------------------------------

        const nameInput =
            document.getElementById("name");

        const phoneInput =
            document.getElementById("phone");

        const addressInput =
            document.getElementById("address");


        const name =
            nameInput ? nameInput.value.trim() : "";

        const phone =
            phoneInput ? phoneInput.value.trim() : "";

        const address =
            addressInput ? addressInput.value.trim() : "";


        // ---------------------------------------------------------
        // Selected meals
        // ---------------------------------------------------------

        const selectedMeals = [];


        document.querySelectorAll(
            "#meals input[type='checkbox']:checked"
        ).forEach((checkbox) => {

            const mealOption =
                checkbox.closest(".meal-option");

            if (!mealOption) return;


            const quantityInput =
                mealOption.querySelector(".meal-quantity");


            let quantity =
                quantityInput
                    ? parseInt(quantityInput.value)
                    : 1;


            if (!quantity || quantity < 1) {

                quantity = 1;

                if (quantityInput) {
                    quantityInput.value = 1;
                }

            }


            selectedMeals.push({
                name: checkbox.value,
                quantity: quantity
            });

        });


        // =========================================================
        // VALIDATION
        // =========================================================

        if (name.length < 3) {

            alert("Name is very short");
            return;

        }


        if (!/^[0-9]{10,11}$/.test(phone)) {

            alert("Invalid Number!");
            return;

        }


        if (address.length < 5) {

            alert("Short address!");
            return;

        }


        if (selectedMeals.length === 0) {

            alert("Please select at least one meal.");
            return;

        }


        // =========================================================
        // SUMMARY
        // =========================================================

        const mealsHTML =
            selectedMeals.map(meal => `
                <li>
                    <span>${meal.name}</span>
                    <strong>× ${meal.quantity}</strong>
                </li>
            `).join("");


        const alertContent =
            document.getElementById("alertContent");

        const customAlert =
            document.getElementById("customAlert");


        if (alertContent) {

            alertContent.innerHTML = `
                <div class="order-summary">

                    <p>
                        <strong>Name:</strong>
                        ${name}
                    </p>

                    <p>
                        <strong>Phone:</strong>
                        ${phone}
                    </p>

                    <p>
                        <strong>Address:</strong>
                        ${address}
                    </p>

                    <p>
                        <strong>Meals:</strong>
                    </p>

                    <ul class="selected-meals">
                        ${mealsHTML}
                    </ul>

                </div>
            `;

        }


        if (customAlert) {
            customAlert.style.display = "flex";
        }

    });

});


// =========================================================
// CONFIRM
// =========================================================

function confirmOrder() {

    const modal =
        document.getElementById("customAlert");


    if (modal) {
        modal.style.display = "none";
    }


    const form =
        document.getElementById("orderForm");


    if (form) {

        HTMLFormElement.prototype.submit.call(form);

    }

}


// =========================================================
// CLOSE
// =========================================================

function closeAlert() {

    const modal =
        document.getElementById("customAlert");


    if (modal) {
        modal.style.display = "none";
    }

}