use std::env;

use actix_web::{get, post, web, App, HttpResponse, HttpServer, Responder};
use rand::{thread_rng, Rng};

#[get("/hello/{name}")]
async fn greet(name: web::Path<String>) -> impl Responder {
    format!("Hello {}!", name)
}

#[derive(serde::Deserialize)]
struct PaymentRequest {
    total_amount: f64,
}

#[derive(serde::Serialize)]
struct PaymentResponse<'a> {
    status: &'a str,
}

#[post("/payment")]
async fn process_payment(payload: web::Json<PaymentRequest>) -> HttpResponse {
    let mut rng = thread_rng();
    let approved = rng.gen_bool(0.8);

    println!(
        "Processed payment of amount {:.2}: {}",
        payload.total_amount,
        if approved { "approved" } else { "declined" }
    );

    if approved {
        HttpResponse::Ok().json(PaymentResponse { status: "success" })
    } else {
        HttpResponse::Ok().json(PaymentResponse { status: "declined" })
    }
}

#[actix_web::main] // or #[tokio::main]
async fn main() -> std::io::Result<()> {
    const DEFAULT_HOST: &str = "127.0.0.1";
    const DEFAULT_PORT: &str = "8080";

    let host = env::var("PAYMENT_API_HOST").unwrap_or_else(|_| DEFAULT_HOST.to_string());
    let port_raw = env::var("PAYMENT_API_PORT").unwrap_or_else(|_| DEFAULT_PORT.to_string());

    let port: u16 = port_raw.parse().unwrap_or_else(|_| {
        eprintln!(
            "Invalid PAYMENT_API_PORT value '{port_raw}'. Falling back to default port {DEFAULT_PORT}."
        );
        DEFAULT_PORT
            .parse()
            .expect("Default port should always parse successfully")
    });

    println!("Starting RustPaymentApi on {host}:{port}");

    HttpServer::new(|| App::new().service(greet).service(process_payment))
        .bind((host.as_str(), port))?
        .run()
        .await
}